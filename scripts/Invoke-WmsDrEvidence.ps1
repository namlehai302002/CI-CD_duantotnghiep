param(
    [string]$BaseUrl = $env:WMS_BASE_URL,
    [string]$SqlcmdConnection = $env:WMS_DR_SQLCMD_CONNECTION,
    [string]$BackupPath = $env:WMS_DR_BACKUP_PATH
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifactDir = Join-Path $root "artifacts\dr"
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$logPath = Join-Path $artifactDir ("dr-evidence-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date))

function Write-Evidence($message) {
    "[{0:yyyy-MM-dd HH:mm:ss}] {1}" -f (Get-Date), $message | Tee-Object -FilePath $logPath -Append
}

function Get-SafeExceptionCode($errorRecord) {
    if ($null -eq $errorRecord -or $null -eq $errorRecord.Exception) {
        return "UnknownError"
    }

    return $errorRecord.Exception.GetType().Name
}

Write-Evidence "DR evidence started. Secrets and connection strings are redacted."

if ($BaseUrl) {
    try {
        $health = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 10
        Write-Evidence "PASS health endpoint status=$($health.StatusCode)"
    } catch {
        Write-Evidence "BLOCKED_OR_FAILED health endpoint :: $(Get-SafeExceptionCode $_)"
    }
} else {
    Write-Evidence "BLOCKED health endpoint :: WMS_BASE_URL not provided"
}

if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
    if ($SqlcmdConnection) {
        try {
            sqlcmd -S $SqlcmdConnection -Q "SELECT 1 AS ConnectivityCheck" -b | Out-File -FilePath (Join-Path $artifactDir "sql-connectivity.out.log") -Encoding UTF8
            Write-Evidence "PASS sql connectivity"
        } catch {
            Write-Evidence "BLOCKED_OR_FAILED sql connectivity :: $(Get-SafeExceptionCode $_)"
        }

        if ($BackupPath) {
            try {
                $escapedBackup = $BackupPath.Replace("'", "''")
                sqlcmd -S $SqlcmdConnection -Q "BACKUP DATABASE [WMS] TO DISK = N'$escapedBackup' WITH COPY_ONLY, INIT, CHECKSUM; RESTORE VERIFYONLY FROM DISK = N'$escapedBackup' WITH CHECKSUM;" -b | Out-File -FilePath (Join-Path $artifactDir "sql-backup-verify.out.log") -Encoding UTF8
                Write-Evidence "PASS backup copy-only and restore verify"
            } catch {
                Write-Evidence "BLOCKED_OR_FAILED backup/restore verify :: $(Get-SafeExceptionCode $_)"
            }
        } else {
            Write-Evidence "BLOCKED backup/restore verify :: WMS_DR_BACKUP_PATH not provided"
        }
    } else {
        Write-Evidence "BLOCKED sql checks :: WMS_DR_SQLCMD_CONNECTION not provided"
    }
} else {
    Write-Evidence "BLOCKED sql checks :: sqlcmd executable not found"
}

Write-Evidence "DR evidence completed. Log=$logPath"
