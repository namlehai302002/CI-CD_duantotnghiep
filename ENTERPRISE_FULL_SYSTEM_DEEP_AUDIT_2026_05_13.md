# Enterprise Full System Deep Audit 2026-05-13

## Audited File Inventory

| Area | Count |
|---|---:|
| Controllers | 43 |
| Services | 40 |
| Models | 70 |
| Data | 1 |
| ViewModels | 5 |
| Views | 129 |
| wwwroot | 19 |
| WMS.Tests | 27 |
| tests | 13 |
| scripts | 5 |
| Migrations | 158 |
| Critical | 0 |

## Appsettings And Sensitive Classification

The `appsettings` file contains operational configuration that must be classified before production. Sensitive paths include:

- ConnectionStrings.DefaultConnection
- Api.Key
- Auth.Smtp.Pass
- GroqApiKey
- GeminiApiKey
- DevResetToken

Secret values are not repeated in this report. For production, store real values in hosting environment settings, a secret store or deployment-specific override and rotate any value that was exposed outside the trusted deployment path.

## Backlog

- Keep `appsettings.json` unchanged in this pass per owner request.
- Add production evidence for secret rotation, visual regression, optional k6 load, backup/restore and DR.
- Finish CurrentStock source-of-truth reconciliation evidence.

