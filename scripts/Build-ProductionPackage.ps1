param(
    [string]$Configuration = "Release",
    [string]$Project = "WMS.csproj",
    [string]$OutputRoot = "artifacts/production-package",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$packageRoot = Join-Path $repoRoot (Join-Path $OutputRoot "wmspro-$stamp")
$publishDir = Join-Path $packageRoot "publish"
$manifestPath = Join-Path $packageRoot "package-manifest.txt"
$hashPath = Join-Path $packageRoot "config-hashes.txt"
$gatePath = Join-Path $packageRoot "package-gate.txt"

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

function Remove-PublishedPath {
    param([string]$RelativePath)

    $target = Join-Path $publishDir $RelativePath
    if (-not (Test-Path -LiteralPath $target)) {
        return
    }

    $resolvedPublish = (Resolve-Path -LiteralPath $publishDir).Path.TrimEnd('\', '/')
    $resolvedTarget = (Resolve-Path -LiteralPath $target).Path
    if (-not $resolvedTarget.StartsWith($resolvedPublish, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove path outside publish directory: $resolvedTarget"
    }

    Remove-Item -LiteralPath $resolvedTarget -Recurse -Force
}

Push-Location $repoRoot
try {
    $publishArgs = @("publish", $Project, "-c", $Configuration, "-o", $publishDir, "/p:UseAppHost=false")
    if ($NoRestore) {
        $publishArgs += "--no-restore"
    }

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    foreach ($relative in @(
        "App_Data",
        "artifacts",
        "appsettings.Development.json",
        "bin",
        "deploy",
        "node_modules",
        "obj",
        "package.json",
        "package-lock.json",
        "publish",
        "test-results",
        "TestResults",
        ".vs",
        ".vscode"
    )) {
        Remove-PublishedPath -RelativePath $relative
    }

    Get-ChildItem -LiteralPath $publishDir -Recurse -File -Filter "*.log" |
        ForEach-Object {
            $resolvedPublish = (Resolve-Path -LiteralPath $publishDir).Path.TrimEnd('\', '/')
            $resolvedTarget = (Resolve-Path -LiteralPath $_.FullName).Path
            if (-not $resolvedTarget.StartsWith($resolvedPublish, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Refusing to remove log outside publish directory: $resolvedTarget"
            }
            Remove-Item -LiteralPath $resolvedTarget -Force
        }

    $files = Get-ChildItem -LiteralPath $publishDir -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            $_.FullName.Substring($publishDir.Length).TrimStart('\', '/') -replace '\\', '/'
        }

    $files | Set-Content -Path $manifestPath -Encoding UTF8

    Get-ChildItem -LiteralPath $repoRoot -Filter "appsettings*.json" -File |
        Sort-Object Name |
        ForEach-Object {
            $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName
            "$($_.Name) $($hash.Hash)"
        } |
        Set-Content -Path $hashPath -Encoding UTF8

    $forbiddenPathPatterns = @(
        '^App_Data(/|$)',
        '^appsettings\.Development\.json$',
        '(^|/)package\.json$',
        '(^|/)package-lock\.json$',
        '(^|/).*\.log$',
        '^App_Data/.*\.log$',
        '^App_Data/auto_backup_config\.json$',
        '^App_Data/DataProtection-Keys(/|$)',
        '^App_Data/uploads(/|$)',
        '(^|/)bin(/|$)',
        '(^|/)obj(/|$)',
        '(^|/)node_modules(/|$)',
        '(^|/)artifacts(/|$)',
        '(^|/)test-results(/|$)',
        '(^|/)TestResults(/|$)',
        '(^|/)playwright-report(/|$)',
        '(^|/)\.vs(/|$)',
        '(^|/)\.vscode(/|$)',
        '(^|/)(secret-dump|secrets-dump|credential-dump|password-dump|local-only)[^/]*\.(txt|json|md|csv|log)$'
    )

    $violations = New-Object System.Collections.Generic.List[string]
    foreach ($file in $files) {
        foreach ($pattern in $forbiddenPathPatterns) {
            if ($file -match $pattern) {
                $violations.Add("Forbidden path pattern [$pattern]: $file")
            }
        }
    }

    $textExtensions = @(".config", ".css", ".csv", ".htm", ".html", ".js", ".json", ".md", ".txt", ".xml", ".yml", ".yaml")
    foreach ($fileInfo in Get-ChildItem -LiteralPath $publishDir -Recurse -File) {
        if ($fileInfo.Name -like "appsettings*.json") {
            continue
        }

        if ($textExtensions -notcontains $fileInfo.Extension.ToLowerInvariant()) {
            continue
        }

        $relative = $fileInfo.FullName.Substring($publishDir.Length).TrimStart('\', '/') -replace '\\', '/'
        $content = Get-Content -LiteralPath $fileInfo.FullName -Raw -ErrorAction SilentlyContinue
        $localUrlPattern = 'https?://(' + 'local' + 'host|' + '127' + '\.0\.0\.1)(:\d+)?'
        if ($content -match $localUrlPattern) {
            $violations.Add("Local development URL found in packaged text file: $relative")
        }
    }

    if ($violations.Count -gt 0) {
        $violations | Set-Content -Path $gatePath -Encoding UTF8
        throw "Production package hygiene gate failed. See $gatePath"
    }

    @(
        "Production package hygiene gate: PASS",
        "Package root: $packageRoot",
        "Manifest: $manifestPath",
        "Config hash evidence: $hashPath",
        "Config values are not printed by this script."
    ) | Set-Content -Path $gatePath -Encoding UTF8

    Write-Host "Production package created: $packageRoot"
    Write-Host "Manifest: $manifestPath"
    Write-Host "Gate: PASS"
}
finally {
    Pop-Location
}
