# Project Overview — MOAT HOUSE HANDOVER v2

This document gives coding agents a compact technical orientation before they modify the repository.

For business rules and product behaviour, `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md` remains authoritative.

## Product purpose

MOAT HOUSE HANDOVER v2 is a local-first Windows desktop handover application for shift-based production operations.

The app supports operational handover, department status capture, labour budget visibility, preview/report generation, and draft-only send workflows.

It is not a demo website and it is not a cloud application.

## Locked architecture

The current architecture is intentionally local-first:

- `desktop-host/` — WPF desktop host, WebView2 shell, host bridge, Windows/runtime integration.
- `webapp/` — HTML/CSS/JavaScript UI rendered inside WebView2.
- Access-oriented persistence/backing services in the desktop host.
- File/folder based attachments and reports.
- Outlook draft-only workflow where applicable.
- Browser/Playwright testing is allowed only as development evidence; it must not replace the Windows desktop runtime.

Do not introduce a web server runtime requirement for end users.
Do not move the product to Electron, cloud hosting, SMTP, or browser-only deployment unless the source of truth is changed by the maintainer.

## Important current source files

Read these before substantial work:

1. `AGENTS.md`
2. `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`
3. `MASTER_TASK_DEEP_REVIEW_UI_UX.md` for deep review/UI correction tasks
4. `README.md`
5. `BUILD_NOTES.md`
6. `docs/TESTING.md`
7. `docs/CODEX_WORKFLOW.md`
8. `docs/AI_TASK_QUEUE.md`

## Runtime surfaces

There are two different execution surfaces:

### Production/runtime surface

The real target is Windows:

- WPF host
- WebView2
- Access/ACE behaviour
- Windows filesystem
- Outlook COM draft flow where implemented

Real workstation validation is still required for these items.

### Browser/mock test surface

The repo now includes Playwright/browser evidence tooling:

- `package.json`
- `playwright.config.mjs`
- `scripts/serve-web-dev.mjs`
- `tests/ui/helpers/mockHostBridge.mjs`
- `scripts/capture-screenshots.mjs`
- `scripts/verify-screenshots.mjs`
- `.github/workflows/ui-evidence.yml`

This is for Codex/CI/browser validation only.
It provides useful UI smoke evidence, but it is not full Windows runtime verification.

## Current high-level workflows

Core app flow, based on source-of-truth direction:

1. Operator/supervisor opens a handover session.
2. Dashboard shows department status and summary indicators.
3. Department screens capture/update operational handover details.
4. Budget view captures/display labour budget information.
5. Preview shows a read-only view of saved handover/budget state.
6. Reports are generated as files.
7. Send flow creates drafts only where implemented; no automatic real email send.

## Admin-only behaviour

Settings and Diagnostics are admin-only tools.
They must not be exposed as normal operator/supervisor workflow actions.

## User identity rule

Normal users must not edit their displayed Windows identity in-app. The app should read identity from Windows/runtime context where applicable.

## Budget rule summary

Budget View is a labour/budget workspace, not an efficiency/yield/downtime dashboard.

Important budget rule:

```text
Variance = Staff Used - Total Staff Required
```

Status mapping:

```text
variance > 0 = over
variance < 0 = under
variance == 0 = on target
```

Budget uses granular labour/budget areas and must not be forced into only the 13 handover departments.

## Design direction

The UI direction is a modern Moat House dark/orange app style:

- dark professional base
- orange brand/header accent
- card/panel based layout
- clear status badges
- dense but readable operational screens
- no large empty bottom gaps on key screens
- no legacy/VBA visual copying
- no raw spreadsheet-like dumps unless explicitly required

## Evidence expectations

For UI changes, provide browser/mock screenshots when possible and clearly label them as browser/mock.

For runtime claims, separate:

- browser/mock verified
- static/build verified
- Windows runtime verified
- not verified
- blocked

Do not claim Windows runtime verification from Playwright/browser screenshots.

## Safe next steps for agents

Prefer small phase PRs:

1. Improve docs/instructions.
2. Improve tests/evidence tooling.
3. Improve one screen or one workflow.
4. Verify with UI evidence and build/package checks.
5. Only then move to the next workflow.

Avoid huge mixed PRs that change backend, UI, reports, security, and documentation all at once unless explicitly requested.
