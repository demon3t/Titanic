param(
    [string]$RepoRoot = (Get-Location).Path,
    [string]$CurrentVersion
)

$messagePattern = '^Deprecated;\s*RemoveIn=(?<removeIn>\d+\.\d+\.\d+);\s*Replacement=(?<replacement>.+)$'
$attributeRegex = [regex]::new('\[Obsolete\("(?<message>[^"]*)"(?:,\s*(?<isError>true|false))?\)\]')
$messageRegex = [regex]::new($messagePattern)

function Resolve-CurrentVersion {
    param([string]$ExplicitVersion)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitVersion)) {
        return $ExplicitVersion
    }

    if (-not [string]::IsNullOrWhiteSpace($env:TITANIC_CURRENT_VERSION)) {
        return $env:TITANIC_CURRENT_VERSION
    }

    $refs = @($env:GITHUB_BASE_REF, $env:GITHUB_REF_NAME, $env:GITHUB_HEAD_REF) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($ref in $refs) {
        if ($ref -match '^release/(?<version>\d+\.\d+\.\d+)$') {
            return $matches['version']
        }
    }

    return $null
}

function Get-CSharpFiles {
    param([string]$Root)

    $rg = Get-Command rg -ErrorAction SilentlyContinue
    if ($rg) {
        $filePaths = & $rg.Source --files $Root -g '*.cs' -g '!**/bin/**' -g '!**/obj/**' -g '!**/.git/**' 2>$null
        foreach ($path in $filePaths) {
            Get-Item -LiteralPath $path -ErrorAction SilentlyContinue
        }

        return
    }

    $separator = [IO.Path]::DirectorySeparatorChar
    Get-ChildItem -Path $Root -Recurse -File -Filter '*.cs' |
        Where-Object {
            $path = $_.FullName
            -not ($path.Contains("${separator}bin${separator}") `
                -or $path.Contains("${separator}obj${separator}") `
                -or $path.Contains("${separator}.git${separator}"))
        }
}

$resolvedCurrentVersion = Resolve-CurrentVersion -ExplicitVersion $CurrentVersion
$currentVersionValue = $null
if (-not [string]::IsNullOrWhiteSpace($resolvedCurrentVersion)) {
    try {
        $currentVersionValue = [version]$resolvedCurrentVersion
    }
    catch {
        Write-Error "Current version '$resolvedCurrentVersion' is not a valid semantic version."
        exit 1
    }
}

$findings = foreach ($file in Get-CSharpFiles -Root $RepoRoot) {
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
        $lineNumber++
        $attributeMatches = $attributeRegex.Matches($line)
        foreach ($attributeMatch in $attributeMatches) {
            $message = $attributeMatch.Groups['message'].Value
            $messageMatch = $messageRegex.Match($message)

            if (-not $messageMatch.Success) {
                [PSCustomObject]@{
                    Kind = 'InvalidFormat'
                    File = $file.FullName
                    Line = $lineNumber
                    Message = $message
                    Details = "Expected: Deprecated; RemoveIn=<version>; Replacement=<alternative|None>"
                }

                continue
            }

            $replacement = $messageMatch.Groups['replacement'].Value.Trim()
            if ([string]::IsNullOrWhiteSpace($replacement)) {
                [PSCustomObject]@{
                    Kind = 'InvalidReplacement'
                    File = $file.FullName
                    Line = $lineNumber
                    Message = $message
                    Details = 'Replacement must be an alternative description or None.'
                }

                continue
            }

            if ($currentVersionValue -ne $null) {
                $removeIn = [version]$messageMatch.Groups['removeIn'].Value
                if ($currentVersionValue -ge $removeIn) {
                    [PSCustomObject]@{
                        Kind = 'ExpiredObsolete'
                        File = $file.FullName
                        Line = $lineNumber
                        Message = $message
                        Details = "Current version $currentVersionValue is greater than or equal to RemoveIn $removeIn."
                    }
                }
            }
        }
    }
}

if (-not $findings) {
    if ($currentVersionValue -eq $null) {
        Write-Host 'Obsolete attributes are formatted correctly. Current version was not resolved; removal deadlines were not evaluated.'
    }
    else {
        Write-Host "Obsolete attributes are formatted correctly. No expired obsolete API for version $currentVersionValue."
    }

    exit 0
}

$findings | Sort-Object File, Line, Kind | Format-Table Kind, File, Line, Message, Details -AutoSize -Wrap
exit 1
