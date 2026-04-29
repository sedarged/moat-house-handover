# Build Notes (Stage 5B Runtime-Readiness)

This repository contains the **Stage 5B runtime-readiness baseline** for MOAT HOUSE HANDOVER v2.

## Implemented scope

- WPF + WebView2 desktop host with startup/runtime initialization.
- Runtime config loading + required key validation.
- Access bootstrap with idempotent schema/seed setup.
- Session open/create/clear and department persistence.
- Attachment metadata + managed file copy flow.
- Budget save/load/recalculate flow.
- Preview assembly from persisted data.
- Report generation + reports folder open action.
- Send package preparation + Outlook draft-only boundary.
- Diagnostics checks + audit log listing.
- Repeatable scripts for prerequisite check, web check, build, package, and package asset verification.

## Local command sequence (Linux/cloud/container)

Run from repository root:

1. `bash scripts/check-prereqs.sh`
2. `bash scripts/check-web.sh`
3. `bash scripts/build-host.sh`
4. `bash scripts/package-local.sh`
5. `bash scripts/verify-package-assets.sh`

If `dotnet` is missing, bootstrap first:

1. `bash scripts/bootstrap-dotnet.sh`
2. re-run the full command sequence above

## Package output

- Local package path: `dist/local-host/`
- Packaged runtime config: `dist/local-host/config/runtime.config.json`
- Packaged web assets: `dist/local-host/webapp/*`

## Runtime dependencies for real workstation validation

Windows runtime verification still requires:

- WebView2 runtime installed
- Access Database Engine / ACE OLEDB provider available
- Outlook desktop configured for draft workflow checks

Use these docs for manual Windows pass execution:

- `docs/LOCAL_WINDOWS_RUNBOOK.md`
- `docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md`

## Verification boundary reminder

Linux/cloud/container checks can verify build/package/static behavior.
They do **not** fully verify:

- interactive WebView2 runtime behavior on Windows
- ACE/OLEDB behavior on target workstation
- Outlook COM draft behavior in a real user profile
- shared folder/permission behavior in workplace environments
