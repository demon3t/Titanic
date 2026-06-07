param(
    [Parameter(Mandatory = $true)]
    [string]$BaseSha,

    [Parameter(Mandatory = $true)]
    [string]$HeadSha
)

$ErrorActionPreference = 'Stop'

function ConvertTo-ProjectXml {
    param(
        [Parameter(Mandatory = $true)]
        [string]$XmlText
    )

    $normalizedXmlText = $XmlText.TrimStart([char]0xFEFF, [char]0x200B, [char]0x2060)
    $projectXml = New-Object System.Xml.XmlDocument
    $projectXml.LoadXml($normalizedXmlText)
    return $projectXml
}

function Get-ProjectVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [string]$GitRef
    )

    if ([string]::IsNullOrWhiteSpace($GitRef)) {
        $projectXml = ConvertTo-ProjectXml -XmlText (Get-Content -LiteralPath $ProjectPath -Raw)
    } else {
        $projectText = @(git show "${GitRef}:$ProjectPath" 2>$null) -join "`n"
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($projectText)) {
            return $null
        }

        $projectXml = ConvertTo-ProjectXml -XmlText $projectText
    }

    $propertyGroups = @($projectXml.Project.PropertyGroup)
    foreach ($propertyGroup in $propertyGroups) {
        if ($propertyGroup.Version) {
            return [string]$propertyGroup.Version
        }

        if ($propertyGroup.PackageVersion) {
            return [string]$propertyGroup.PackageVersion
        }
    }

    return $null
}

function Test-PackageAffected {
    param(
        [string[]]$ChangedFiles,
        [string[]]$WatchedPrefixes
    )

    foreach ($changedFile in $ChangedFiles) {
        foreach ($prefix in $WatchedPrefixes) {
            if ($changedFile.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
    }

    return $false
}

$null = git rev-parse --verify $BaseSha
if ($LASTEXITCODE -ne 0) {
    throw "Base commit '$BaseSha' was not found."
}

$null = git rev-parse --verify $HeadSha
if ($LASTEXITCODE -ne 0) {
    throw "Head commit '$HeadSha' was not found."
}

$changedFiles = @(git diff --name-only $BaseSha $HeadSha)

$packages = @(
    @{
        Name = 'Titanic.Db'
        ProjectPath = 'Titanic.Db/Titanic.Db.csproj'
        WatchedPrefixes = @('Titanic.Common/', 'Titanic.Db/')
    },
    @{
        Name = 'Titanic.Entity'
        ProjectPath = 'Titanic.Entity/Titanic.Entity.csproj'
        WatchedPrefixes = @('Titanic.Common/', 'Titanic.Db/', 'Titanic.Entity/')
    }
)

$plan = [ordered]@{
    baseSha = $BaseSha
    headSha = $HeadSha
    changedFiles = $changedFiles
    packages = @()
}

$errors = New-Object System.Collections.Generic.List[string]

foreach ($package in $packages) {
    $affected = Test-PackageAffected -ChangedFiles $changedFiles -WatchedPrefixes $package.WatchedPrefixes
    $currentVersion = Get-ProjectVersion -ProjectPath $package.ProjectPath
    $previousVersion = Get-ProjectVersion -ProjectPath $package.ProjectPath -GitRef $BaseSha
    $versionChanged = $affected -and -not [string]::IsNullOrWhiteSpace($currentVersion) -and ($currentVersion -ne $previousVersion)

    if ($affected -and -not $versionChanged) {
        $errors.Add("Package '$($package.Name)' is affected by this release, but its version was not bumped. Previous='$previousVersion', current='$currentVersion'.")
    }

    $plan.packages += [pscustomobject]@{
        name = $package.Name
        projectPath = $package.ProjectPath
        affected = $affected
        previousVersion = $previousVersion
        currentVersion = $currentVersion
        versionChanged = $versionChanged
    }
}

$planJson = $plan | ConvertTo-Json -Depth 6
$planJson

if ($env:GITHUB_OUTPUT) {
    foreach ($package in $plan.packages) {
        $key = $package.name.Replace('.', '_').ToLowerInvariant()
        Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "$($key)_affected=$($package.affected.ToString().ToLowerInvariant())"
        Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "$($key)_version=$($package.currentVersion)"
    }

    $hasAffected = [bool]($plan.packages | Where-Object affected)
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "has_affected=$($hasAffected.ToString().ToLowerInvariant())"
}

if ($errors.Count -gt 0) {
    foreach ($errorText in $errors) {
        Write-Error $errorText
    }

    exit 1
}
