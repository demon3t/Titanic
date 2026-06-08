param(
    [string]$RepoRoot = (Get-Location).Path,
    [switch]$PathsOnly,
    [switch]$FirstMatchPerFile
)

$patterns = @('*.cs', '*.md', '*.json', '*.ps1', '*.toml', '*.yml', '*.yaml')
$replacementChar = [string][char]0xFFFD
$latinEthUpper = [string][char]0x00D0
$latinEnyeUpper = [string][char]0x00D1

$escapedReplacement = [regex]::Escape($replacementChar)
$escapedEth = [regex]::Escape($latinEthUpper)
$escapedEnye = [regex]::Escape($latinEnyeUpper)
$questionMarkRun = '\?{4,}'
$suspiciousPattern = "$escapedReplacement|$escapedEth.|$escapedEnye.|$questionMarkRun"
$regex = [regex]::new($suspiciousPattern)
$utf8 = [System.Text.UTF8Encoding]::new($false, $true)
$cp1251 = [System.Text.Encoding]::GetEncoding(1251)

$rg = Get-Command rg -ErrorAction SilentlyContinue
if ($rg) {
    $rgArgs = @('--files', $RepoRoot)
    foreach ($pattern in $patterns) {
        $rgArgs += @('-g', $pattern)
    }
    $rgArgs += @('-g', '!**/bin/**', '-g', '!**/obj/**', '-g', '!**/.git/**')
    $filePaths = & $rg.Source @rgArgs 2>$null

    $files = foreach ($path in $filePaths) {
        Get-Item -LiteralPath $path -ErrorAction SilentlyContinue
    }
}
else {
    $separator = [IO.Path]::DirectorySeparatorChar
    $files = Get-ChildItem -Path $RepoRoot -Recurse -File -Include $patterns |
        Where-Object {
            $path = $_.FullName
            -not ($path.Contains("${separator}bin${separator}") `
                -or $path.Contains("${separator}obj${separator}") `
                -or $path.Contains("${separator}.git${separator}"))
        }
}

$findings = foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $text = $null

    try {
        $text = $utf8.GetString($bytes)
    }
    catch {
        $preview = ($cp1251.GetString($bytes) -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
        if (-not $preview) {
            $preview = '<empty>'
        }

        [PSCustomObject]@{
            Kind = 'InvalidUtf8'
            File = $file.FullName
            Line = 1
            Column = 1
            Sample = 'File is not UTF-8 encoded'
            Text = $preview.Trim()
        }

        continue
    }

    $lineNumber = 0
    $matchedInFile = $false
    foreach ($line in ($text -split "`r?`n")) {
        $lineNumber++
        $matches = $regex.Matches($line)
        foreach ($match in $matches) {
            $matchedInFile = $true
            [PSCustomObject]@{
                Kind = 'SuspiciousText'
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

$findings | Sort-Object File, Line, Column | Format-Table Kind, File, Line, Column, Sample, Text -AutoSize -Wrap
exit 1
