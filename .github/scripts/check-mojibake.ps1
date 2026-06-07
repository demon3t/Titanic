param(
    [string]$RepoRoot = (Get-Location).Path,
    [switch]$PathsOnly,
    [switch]$FirstMatchPerFile
)

$patterns = @('*.cs', '*.md', '*.json', '*.ps1', '*.toml', '*.yml', '*.yaml')
$replacementChar = [string][char]0xFFFD
$cyrillicEr = [string][char]0x0420
$cyrillicEs = [string][char]0x0421
$latinEthUpper = [string][char]0x00D0
$latinEnyeUpper = [string][char]0x00D1

$escapedReplacement = [regex]::Escape($replacementChar)
$escapedEr = [regex]::Escape($cyrillicEr)
$escapedEs = [regex]::Escape($cyrillicEs)
$escapedEth = [regex]::Escape($latinEthUpper)
$escapedEnye = [regex]::Escape($latinEnyeUpper)
$suspiciousPattern = "$escapedReplacement|$escapedEr[А-Яа-яЁё]$escapedEs[А-Яа-яЁё]|$escapedEth.|$escapedEnye."
$regex = [regex]::new($suspiciousPattern)

$separator = [IO.Path]::DirectorySeparatorChar
$files = Get-ChildItem -Path $RepoRoot -Recurse -File -Include $patterns |
    Where-Object {
        $path = $_.FullName
        -not ($path.Contains("${separator}bin${separator}") `
            -or $path.Contains("${separator}obj${separator}") `
            -or $path.Contains("${separator}.git${separator}"))
    }

$findings = foreach ($file in $files) {
    $lineNumber = 0
    $matchedInFile = $false
    foreach ($line in Get-Content $file.FullName -ErrorAction SilentlyContinue) {
        $lineNumber++
        $matches = $regex.Matches($line)
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
