# Release Evidence 2026-05-20

System: WMS Pro  
Release type: enterprise/internal world-class hardening evidence pack  
Secret rule: config values and secret values are never printed; only file hashes and key names may be recorded.

## Build Evidence

- Command: `dotnet build WMS.sln -c Debug --no-restore`
- Result: passed, `0 Warning(s)`, `0 Error(s)`.
- Failure condition: any build error or new warning in changed scope.

## Test Evidence

- Command: `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"`
- Result: passed, `540 passed`, `0 failed`, `0 skipped`.
- Failure condition: any failed test.

## Vulnerability Scan

- Command: `dotnet list WMS.csproj package --vulnerable --include-transitive --no-restore`
- Result: passed, no vulnerable packages reported by the current NuGet source.
- Failure condition: vulnerable package reported.

## Migration List

- Command: `dotnet ef migrations list --no-build --project WMS.csproj --startup-project WMS.csproj`
- Result: passed, migration list returned through `20260519233007_WidenAuditLogActionType`.
- Failure condition: unexpected migration error or pending migration not acknowledged.

## Config Hash Evidence

- `appsettings.json`: `8774FCA21C5C3300F66E3E8A9959E391ECE69329F753D4DDED6C08E7C809DE9B`
- `appsettings.Development.json`: `BF7CDBC1C0E2675EBFF84AA388E19B1A978262BBC5B89D7B4E43FC56B26471E6`
- Values: not printed, not copied, not redacted, not modified.

## Packaging Manifest

- Script: `scripts/Build-ProductionPackage.ps1`
- Expected artifact: `artifacts/production-package/<timestamp>/package-manifest.txt`
- Gate: fail on runtime logs, upload data, generated folders, local dev output, secret dump files, and loopback host URLs outside preserved config files.
- Result: passed for `artifacts/production-package/wmspro-20260520-100111/package-manifest.txt`.

## Backup/Restore Drill

- Status: pending external evidence.
- Required evidence: backup start/end time, restore target, validation queries, row counts, operator, rollback decision, and elapsed time.
- Failure condition: restore cannot validate inventory, voucher, user, audit, and migration history tables.

## Visual Regression Evidence

- Status: pending external evidence.
- Required command: Playwright suite under `tests/visual`.
- Required coverage: auth, dashboard, inbound, outbound, inventory, users, 3PL, yard, reports, desktop 100/110/125 percent, mobile.
- Failure condition: overlap, clipped text, broken navigation, missing auth state, or blank screenshots.

## k6 Load Evidence

- Status: pending external evidence; optional for local UI/functional green gate.
- Optional command: `k6 run tests/load/k6-wms-dod.js`.
- Required coverage: inventory reads, scan queue retry, large reports, 3PL billing, integration API.
- Failure condition: login fallback, failed SLA threshold, duplicate mutation in mutation-enabled staging mode, or growing queue after load stops.

## Security Scope Scan

- Registry: `docs/EXPORT_DOWNLOAD_API_SCOPE_REGISTRY.md`
- Required evidence: every export/download/API read action maps authorization, warehouse scope, owner scope, audit, anti-forgery applicability, and no cross-owner leakage gate.
- Result: passed through `WorldClassCompletionGateTests.ExportDownloadApiScopeRegistry_ShouldCoverEverySensitiveReadSurface`.

## Rollback Notes

- This hardening pass should not change database schema.
- Rollback for docs/tests/script/view placeholder is file-level revert only.
- Runtime data, logs, uploads, and config values are not modified by this release.


