# Production Migration Validation

## Dry Run

- Chay `dotnet ef migrations list --no-build` de xac nhan khong co migration thieu trong ma nguon.
- Tao script idempotent bang `dotnet ef migrations script --idempotent -o App_Data/migration-idempotent.sql`.
- Ra soat script truoc khi chay production: bang moi, index moi, foreign key, thoi gian khoa bang.
- Chay tren staging restore tu backup moi nhat truoc khi chay production.

## Rollback Plan

- Backup DB ngay truoc release.
- Neu migration loi truoc khi app start: restore DB backup va rollback artifact ung dung.
- Neu app loi sau migration: rollback app truoc, giu DB neu schema tuong thich; neu khong tuong thich thi restore backup.
- Ghi ro migration cuoi cung da apply va migration rollback target.

## Seed And Drift Validation

- Chay smoke test dang nhap, `/health`, trang chinh, ton kho, phieu nhap/xuat, BI, SRE.
- Kiem tra seed role/permission quan trong: Admin, Manager, Staff, Viewer, `report.view`, `report.view.financial`, `system.danger.ops`.
- So sanh `dotnet ef migrations list --no-build` sau update de xac nhan khong con pending.
- Ghi evidence: nguoi chay, thoi gian, DB target, app version, ket qua build/test/migration.

