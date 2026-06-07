param(
    [string]$RepoRoot = (Get-Location).Path,
    [switch]$PathsOnly,
    [switch]$FirstMatchPerFile
)

$patterns = @('*.cs', '*.md', '*.json', '*.ps1', '*.toml', '*.yml', '*.yaml')
$replacementChar = [string][char]0xFFFD
$replacementRegex = [regex]::new([regex]::Escape($replacementChar))
$utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)

$separator = [IO.Path]::DirectorySeparatorChar
$files = Get-ChildItem -Path $RepoRoot -Recurse -File -Include $patterns |
    Where-Object {
        $path = $_.FullName
        -not ($path.Contains("${separator}bin${separator}") `
            -or $path.Contains("${separator}obj${separator}") `
            -or $path.Contains("${separator}.git${separator}"))
    }

$findings = foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)

    try {
        $content = $utf8Strict.GetString($bytes)
    }
    catch {
        [PSCustomObject]@{
            File = $file.FullName
            Line = 1
            Column = 1
            Sample = 'Invalid UTF-8'
            Text = 'File is not valid UTF-8.'
        }
        continue
    }

    $lineNumber = 0
    $matchedInFile = $false
    foreach ($line in ($content -split "`r?`n")) {
        $lineNumber++
        $matches = $replacementRegex.Matches($line)
        foreach ($match in $matches) {
            $matchedInFile = $true
            [PSCustomObject]@{
                File = $file.FullName
                Line = $lineNumber
                Column = $match.Index + 1
                Sample = $match.Value
                Text = $line.Trim()
            }
        }

        if ($FirstMatchPerFile -and $matchedInFile) {
            break
        }
    }
}

if (-not $findings) {
    Write-Host 'No suspicious mojibake patterns found.'
    exit 0
}

if ($PathsOnly) {
    $findings | Select-Object -ExpandProperty File -Unique | Sort-Object
    exit 1
}

$findings | Sort-Object File, Line, Column | Format-Table -AutoSize
exit 1
