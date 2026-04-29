# MOAT HOUSE HANDOVER v2

Local-first Windows desktop handover application for Moat House shift operations.

The active source of truth is:

- `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`
- `AGENTS.md`
- `MASTER_TASK_DEEP_REVIEW_UI_UX.md` for the current deep review / UI correction workflow

Older fragmented plan/spec/reference files have been retired. Do not use deleted `01_*` to `07_*` files as source of truth.

## Product direction

MOAT HOUSE HANDOVER v2 is a modern replacement for the old workbook/UserForm workflow while preserving the real business process:

- open AM / PM / NS shift sessions by date
- record department handover status and notes
- record production metrics for selected metric departments
- manage department attachments/evidence
- record labour budget planned vs used
- preview saved handover data
- generate local handover and budget reports
- prepare an Outlook draft package
- run admin-only diagnostics and audit checks

## Architecture

- `desktop-host/` — WPF + WebView2 desktop host
- `webapp/` — HTML/CSS/JS application UI
- `backend/access/` — Access schema and seed/bootstrap artifacts
- `config/` — runtime configuration template/assets
- `docs/` — practical runbooks and runtime/manual testing guidance
- `scripts/` — repeatable checks, build, bootstrap, package, and verification scripts
- `.github/workflows/` — Windows build/package CI

The architecture is locked as:

- local-first Windows desktop application
- WPF desktop shell
- WebView2 UI
- Access-oriented backend
- attachments stored as files/folders
- reports stored as files/folders
- Outlook draft-only workflow
- no automatic email sending
- no cloud database
- no local web server requirement

## Current implemented baseline

The repository currently contains implementation for:

- runtime config loading
- startup validation and folder checks
- Access bootstrap/schema path
- WebView2 static asset loading without local web server
- session lifecycle
- department persistence
- attachment bridge/actions and metadata persistence
- budget persistence and dashboard summary
- preview payload and rendering
- local report generation foundation
- send package preparation
- Outlook draft-only boundary
- best-effort audit logging
- runtime diagnostics/admin checks
- Windows CI build/package validation

See `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md` for the active behaviour requirements and the current target direction.

## Helper scripts

Use repository helper scripts instead of ad hoc commands where possible.

- `scripts/bootstrap-dotnet.sh`
  - Installs local .NET SDK into `./.dotnet` when needed in cloud/dev environments.

- `scripts/check-prereqs.sh`
  - Confirms required CLI tools are present.

- `scripts/check-web.sh`
  - Verifies `webapp/index.html` exists and runs `node --check` over JS files.

- `scripts/build-host.sh`
  - Builds `desktop-host/MoatHouseHandover.Host.csproj` in Release mode with Windows targeting enabled.

- `scripts/package-local.sh`
  - Publishes the desktop host into `dist/local-host` with Windows targeting enabled.

- `scripts/verify-package-assets.sh`
  - Verifies packaged `webapp/index.html` and `config/runtime.config.json` exist in publish output.

## Agent working model

Agents must read:

1. `AGENTS.md`
2. `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`
3. `MASTER_TASK_DEEP_REVIEW_UI_UX.md` when doing review/correction work

Agents must not rely on retired fragmented specs or older PR comments when those conflict with the active source of truth.

## Windows CI

GitHub Actions includes a Windows workflow at `.github/workflows/windows-build.yml` for pull requests and manual dispatch.

It verifies:

- Web syntax checks with `node --check`
- Windows-targeted desktop host build
- local package publish output under `dist/local-host`
- packaged asset presence checks
- package artifact upload

It does not fully verify:

- Outlook COM draft creation on a real workstation
- ACE/OLEDB runtime installation and behavior
- interactive WebView2 user workflows
- real shared-folder/network permission behavior

Windows CI is build/package validation, not a replacement for real workstation runtime verification.
