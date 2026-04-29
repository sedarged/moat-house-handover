# desktop-host/AGENTS.md — Desktop Host Agent Rules

This file applies to all work under `desktop-host/`.

Read first:

- `../AGENTS.md`
- `../HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`
- `../docs/PROJECT_OVERVIEW.md`
- `../docs/TESTING.md`
- `../docs/CODEX_WORKFLOW.md`

## Runtime boundary

`desktop-host/` is the real Windows runtime surface.

Preserve the locked local-first architecture:

- WPF desktop host
- WebView2 shell
- Access/ACE/OLEDB persistence where currently used
- Windows filesystem integration
- local report generation
- Outlook draft-only flow where implemented

Do not replace this with Electron, a cloud service, a production web server, SMTP email sending, or browser-only runtime unless the source of truth is explicitly changed by the maintainer.

Browser/Playwright mock evidence does not prove desktop-host runtime behaviour.

## C# / host service rules

- Keep host contracts, services, repositories, and web bridge payloads aligned.
- Use migration-safe schema changes.
- Preserve Access compatibility.
- Do not break existing host bridge message names without updating all web callers, tests, and docs.
- Keep reports local-file based.
- Do not add automatic real email sending.
- Prefer explicit DTO/contract updates over anonymous/dynamic payload guessing.

## Security and runtime safety

- Validate paths before file operations.
- Keep file operations inside configured/root folders.
- Do not trust attachment names, report paths, user-entered text, department names, comments, or reason text.
- Guard against path traversal.
- Escape report HTML/user-entered text.
- Report blocked runtime dependencies clearly.

## Budget / Preview / Report alignment

Budget variance rule:

```text
Variance = Staff Used - Total Staff Required
```

Status mapping:

```text
variance > 0 = over
variance < 0 = under
variance == 0 = on target
```

If host-side budget, preview, or report contracts change, inspect and update the matching web files:

- `webapp/js/services/*.js`
- `webapp/js/screens/*.js`
- `tests/ui/helpers/mockHostBridge.mjs`
- relevant UI tests

## Verification for desktop-host changes

Run the relevant host checks:

```bash
bash scripts/check-prereqs.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If `.NET` is missing and the helper exists, run:

```bash
bash scripts/bootstrap-dotnet.sh
bash scripts/check-prereqs.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If a host/bridge change can affect the web UI, also run:

```bash
npm run check:web
npm run test:ui
npm run screenshots
npm run verify:screenshots
```

## Windows runtime honesty

Only claim Windows runtime verification when the app was actually launched and tested on a Windows workstation with the real WPF/WebView2 host and runtime dependencies.

If not tested on Windows, say so.

Use these categories in reports:

- Static/syntax verified
- Browser/mock verified
- Build/package verified
- Windows runtime verified
- Not verified
- Blocked
