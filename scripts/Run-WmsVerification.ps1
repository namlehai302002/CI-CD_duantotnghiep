param(
    [int]$Port = 5299,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipVisual,
    [switch]$UpdateVisualBaselines,
    [switch]$SkipK6,
    [switch]$IncludeK6,
    [switch]$InstallK6IfMissing
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifactDir = Join-Path $root "artifacts\verification"
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

function Write-Step($message) {
    $line = "[{0:yyyy-MM-dd HH:mm:ss}] {1}" -f (Get-Date), $message
    $line | Tee-Object -FilePath (Join-Path $artifactDir "verification.log") -Append
}

function Get-SafeExceptionCode($errorRecord) {
    if ($null -eq $errorRecord -or $null -eq $errorRecord.Exception) {
        return "UnknownError"
    }

    return $errorRecord.Exception.GetType().Name
}

function Invoke-Logged($name, [scriptblock]$script) {
    Write-Step "START $name"
    try {
        $global:LASTEXITCODE = 0
        & $script
        if ($global:LASTEXITCODE -ne 0) {
            throw "$name exited with code $global:LASTEXITCODE"
        }
        Write-Step "PASS $name"
    } catch {
        Write-Step "BLOCKED_OR_FAILED $name :: $(Get-SafeExceptionCode $_)"
        throw
    }
}

function Assert-CleanWmsServerLogs([string[]]$paths) {
    $existingPaths = @($paths | Where-Object { $_ -and (Test-Path $_) })
    if ($existingPaths.Count -eq 0) {
        Write-Step "SKIP local server log noise gate :: no local server log files were produced."
        return
    }

    $patterns = @(
        '^\s*fail:',
        "Skip'/'Take' without an 'OrderBy'",
        'only implements IAsyncDisposable',
        'Unable to record request telemetry',
        'An error occurred using a transaction'
    )

    $hits = @()
    foreach ($path in $existingPaths) {
        foreach ($pattern in $patterns) {
            $hits += Select-String -Path $path -Pattern $pattern -ErrorAction SilentlyContinue |
                ForEach-Object { "{0}:{1}: {2}" -f (Split-Path $_.Path -Leaf), $_.LineNumber, $_.Line.Trim() }
        }
    }

    if ($hits.Count -gt 0) {
        $out = Join-Path $artifactDir "server-log-noise.txt"
        $hits | Set-Content -Path $out -Encoding UTF8
        throw "Local server log noise gate found $($hits.Count) blocking line(s). See $out"
    }

    Write-Step "PASS local server log noise gate :: no fail/EF paging/telemetry/dispose noise found."
}

Push-Location $root
try {
    if (-not $SkipBuild) {
        Invoke-Logged "dotnet build" { dotnet build WMS.sln -c Debug --no-restore }
    }

    if (-not $SkipTests) {
        Invoke-Logged "dotnet test" { dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal" }
    }

    $baseUrl = $env:WMS_BASE_URL
    $server = $null
    $serverOut = $null
    $serverErr = $null
    if (-not $baseUrl) {
        while (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) {
            $Port++
        }
        $loopback = [System.Net.IPAddress]::Loopback.ToString()
        $baseUrl = "http://$loopback`:$Port"
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:WMS_BASE_URL = $baseUrl
        $serverOut = Join-Path $artifactDir "local-server-$Port.out.log"
        $serverErr = Join-Path $artifactDir "local-server-$Port.err.log"
        $server = Start-Process dotnet -ArgumentList @("run", "--no-build", "--urls", $baseUrl) -WorkingDirectory $root -RedirectStandardOutput $serverOut -RedirectStandardError $serverErr -WindowStyle Hidden -PassThru
        for ($i = 0; $i -lt 60; $i++) {
            try {
                $response = Invoke-WebRequest -Uri "$baseUrl/health" -UseBasicParsing -TimeoutSec 2
                if ($response.StatusCode -lt 500) { break }
            } catch {
                Start-Sleep -Seconds 1
            }
            if ($i -eq 59) { throw "Local server did not become ready at $baseUrl" }
        }
    }

    if (-not $SkipVisual) {
        Invoke-Logged "visual public" { npm run visual:public }
        Invoke-Logged "visual auth" { npm run visual:auth }
        if ($UpdateVisualBaselines) {
            Invoke-Logged "visual update baselines" { npm run visual:update }
        }
        Invoke-Logged "visual test" { npm run visual:test }
        Invoke-Logged "visual no-device RF/print evidence" { npm run visual:no-device }
        Invoke-Logged "visual mobile deep evidence" { npm run visual:mobile-deep }
    }

    if ($server) {
        Invoke-Logged "local server log noise gate" { Assert-CleanWmsServerLogs @($serverOut, $serverErr) }
    } else {
        Write-Step "SKIP local server log noise gate :: WMS_BASE_URL was provided externally."
    }

    if ($SkipK6) {
        Write-Step "SKIP optional k6 :: -SkipK6 was provided."
    } elseif ($IncludeK6) {
        if (-not (Get-Command k6 -ErrorAction SilentlyContinue)) {
            if ($InstallK6IfMissing -and (Get-Command winget -ErrorAction SilentlyContinue)) {
                Invoke-Logged "install k6" { winget install k6.k6 --silent --accept-package-agreements --accept-source-agreements }
            }
        }

        if (Get-Command k6 -ErrorAction SilentlyContinue) {
            $env:WMS_K6_SUMMARY_PATH = Join-Path $artifactDir "k6-summary-100.json"
            Invoke-Logged "k6 load profile 100" { k6 run tests/load/k6-wms-dod.js }
        } else {
            Write-Step "OPTIONAL_BLOCKED k6 :: k6 executable not found. Re-run with -IncludeK6 -InstallK6IfMissing or install k6 manually."
        }
    } else {
        Write-Step "SKIP optional k6 :: use -IncludeK6 to run the load profile."
    }
} finally {
    if ($server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force
    }
    Pop-Location
}
