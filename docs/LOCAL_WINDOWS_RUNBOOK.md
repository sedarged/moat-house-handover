# Local Windows Runtime Runbook (Stage 5B)

This runbook is for controlled workstation testing of **MOAT HOUSE HANDOVER v2**.

Scope reminder:
- Local-first Windows desktop host (WPF + WebView2)
- Access-oriented backend
- File-based attachments/reports
- Outlook **draft-only** behavior (no automatic send)

## 1) Get the packaged app

Use the repository packaging flow:

1. Build/package from repo root:
   - `scripts/package-local.sh`
2. Verify packaged assets:
   - `scripts/verify-package-assets.sh`

Package output folder:
- `dist/local-host/`

The packaged runtime config is expected at:
- `dist/local-host/config/runtime.config.json`

The packaged web assets are expected at:
- `dist/local-host/webapp/*`

## 2) Runtime dependencies on the Windows workstation

Expected dependencies for real runtime checks:
- Windows workstation runtime
- WebView2 Runtime installed
- Access Database Engine / ACE provider available for OLEDB path
- Outlook desktop installed/configured for draft workflow tests

If any dependency is missing, stop and note it in the test report.

## 3) Run locally

1. Copy `dist/local-host` to the workstation.
2. Confirm `config/runtime.config.json` points to expected local/shared paths.
3. Launch `MoatHouseHandover.Host.exe`.
4. Confirm Shift screen appears.

## 4) Run Diagnostics first

1. From Shift screen or Dashboard, open **Diagnostics**.
2. Click **Run Diagnostics**.
3. Review overall status + each check.
4. Use **Open Logs Folder** if any check fails.

Diagnostics should explicitly confirm/check:
- runtime OS boundary (Windows vs non-Windows)
- Access DB path and Access connection open
- attachments/reports/log roots and write access
- email profile mapping for AM/PM/NS
- Outlook COM availability (draft support prerequisite)

## 5) Folders to verify during runtime testing

Check these roots from runtime config and diagnostics output:
- Access DB location (`accessDatabasePath`)
- Attachments root (`attachmentsRoot`)
- Reports root (`reportsOutputRoot`)
- Logs root (`logRoot` or default logs folder near config)

## 6) Safe test data notes (non-destructive)

Use manual test setup only:
- Open AM session for today.
- If no session exists, create blank session when prompted.
- Enter small sample values through normal UI flow.

Do **not** clear or reset shared production tables as part of routine testing.

## 7) Failure reporting package (what testers should send back)

For every failure, send:
1. Screenshot of visible error/state.
2. Diagnostics results (overall + failed/warning checks).
3. Relevant log file from logs folder.
4. Exact reproduction steps and the exact step number where it failed.

Use the step numbering from `docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md`.

## 8) Important boundaries

- Outlook action is **draft creation only**.
- Email must **not** be automatically sent by this app.
- Linux/cloud checks are partial only; final runtime confidence requires real Windows execution.
