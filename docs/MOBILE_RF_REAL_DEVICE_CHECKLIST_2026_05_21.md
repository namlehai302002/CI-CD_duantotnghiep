# Mobile RF Real Device Checklist - 2026-05-21

Use this checklist on a real warehouse device before production sign-off. Do not record passwords, API keys, cookies, or customer data in screenshots.

## Device Matrix

- Android phone with Chrome.
- iPhone or iPad with Safari.
- USB or Bluetooth barcode scanner paired to a workstation.
- Mobile camera scan on receiving and picking pages.
- Label/document printer used by operations.

## RF And Scanner Smoke

- Login with the local/staging verification account, then confirm production users still require normal authentication.
- Open RF receiving, scan or type an ASN/voucher, item barcode, location, lot, expiry, and quantity.
- Open RF picking, scan task, source location, item/package code, and confirm picked quantity.
- Open RF movement, scan source, item/LPN, destination location, and confirm movement.
- Toggle offline mode or network interruption; queue one operation, restore network, and confirm the queue drains once.
- Confirm offline queue badge appears only on operational pages, not account/auth pages.

## Mobile Layout

- Check desktop zoom 100%, 110%, 125% and mobile portrait.
- Confirm no text overlaps, no vertical crushed table headers, no hidden primary action.
- Confirm scanner modal fits viewport and close/focus behavior works.
- Confirm sticky action/footer controls do not cover form fields.

## Print

- Print item labels, shipping handover, voucher document, and customer label.
- Confirm barcode is readable by scanner.
- Confirm print CSS uses external stylesheet, not local view `local-style block`.

## Evidence

- Record tester, device, browser, date/time, environment, and pass/fail result.
- Store screenshots/logs under `artifacts/mobile-rf/`.
- List blocked items with reason and owner.

## Local No-Device Substitute

When physical scanner/printer hardware is not available, run:

```powershell
npm run visual:no-device
```

This creates local evidence for keyboard-wedge scan readiness, camera modal fit, and label print preview rendering. Mark the real scanner/printer rows above as pending external hardware evidence instead of failing the local gate.

