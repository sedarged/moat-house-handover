# MOAT HOUSE HANDOVER v2

Stage 5B runtime-readiness baseline for a local-first Windows desktop handover application.

## Repository Structure

- `desktop-host/` — WPF + WebView2 desktop host with runtime bootstrap
- `webapp/` — HTML/CSS/JS application shell and placeholder screens
- `backend/access/` — Access schema and seed artifacts used by host bootstrap
- `docs/` — stage notes and continuation guidance
- `docs/LOCAL_WINDOWS_RUNBOOK.md` — practical workstation run/test instructions
- `scripts/` — repeatable helper scripts for checks, build, SDK bootstrap, and local packaging
- `BUILD_NOTES.md` — local build and runtime notes

## Stage 2E Extended Scope Completed
- Local runtime config loading from JSON with safe path fallback strategy
- Startup validation for required runtime config values
- Startup initialization service (folder checks + Access bootstrap + logging)
- Idempotent Access schema + approved seed bootstrap path
- Deployment-safe local web asset resolution without local web server
- Session + department persistence (Stage 2B/2C)
- Attachment bridge actions and Access-backed metadata persistence (`attachment.list`, `attachment.add`, `attachment.remove`, `attachment.openViewer`)
- Host-side local file picker/copy flow (`file.pickFile`)
- Department attachment panel + viewer flow + dashboard attachment counts
- Budget header/row persistence (`budget.load`, `budget.save`, `budget.recalculate`) with per-session row seeding
- Dashboard budget summary from persisted Access-backed totals (`dashboard.budgetSummary`)
- Budget screen edit/recalculate/save/reload flow tied to active session
- Real persisted Preview payload + screen rendering (`preview.load`)
- Local HTML report generation foundation (`reports.generateHandover`, `reports.generateBudget`, `reports.generateAll`)
- Session-scoped reports folder open action (`shell.openReportsFolder`)
- Shift-based email profile loading from Access (`emailProfile.loadForShift`)
- Send package preparation with validation (`send.preparePackage`)
- Outlook draft-only creation boundary (`send.createOutlookDraft`) with graceful non-Windows/Outlook handling
- Real Send screen for package preparation, validation display, and draft creation workflow
- Host-side audit logging wired to key workflow actions (`tblAuditLog`) with best-effort/no-crash behavior
- Runtime diagnostics service + bridge actions (`diagnostics.run`, `audit.listRecent`, `audit.listForSession`)
- Diagnostics screen (run checks, view overall status, inspect recent audit entries)
- Operational hardening of send/report validation and failure messaging

## Still Deferred (by design)
- Actual email send action (auto/manual send remains intentionally deferred)
- Windows runtime validation of WebView2 image rendering + ACE/OLEDB behavior

## Helper scripts
These scripts are intended for both **AI agents** and **human reviewers** so repository checks/builds can be repeated consistently.


- `scripts/bootstrap-dotnet.sh`
  - Use when `dotnet` is unavailable in cloud/dev environments.
  - Installs a local .NET SDK into `./.dotnet` (idempotent) so build/package scripts can run without system-wide installation.

- `scripts/check-prereqs.sh`
  - Use when first entering the repo.
  - Confirms required CLI tools are present (`git`, `node`, `dotnet`), including local `./.dotnet/dotnet` when bootstrapped.

- `scripts/check-web.sh`
  - Use before/after web-layer edits.
  - Verifies `webapp/index.html` exists and runs `node --check` over JS files.

- `scripts/build-host.sh`
  - Use when validating host compilation.
  - Builds `desktop-host/MoatHouseHandover.Host.csproj` in Release mode with Windows targeting enabled.

- `scripts/package-local.sh`
  - Use when preparing a local distributable output for review.
  - Publishes the desktop host into `dist/local-host` with Windows targeting enabled.

- `scripts/verify-package-assets.sh`
  - Use after packaging.
  - Verifies packaged `webapp/index.html` and `config/runtime.config.json` exist in the publish output.

## Codex working model
- Spec files (`01_*` through `07_*`) are the source of truth.
- Root `AGENTS.md` instructions must be respected.
- Prefer repository helper scripts over ad hoc commands wherever possible.
- For real workstation verification flow, follow `docs/LOCAL_WINDOWS_RUNBOOK.md` and `docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md`.

## Windows CI

GitHub Actions now includes a Windows workflow at `.github/workflows/windows-build.yml` for pull requests and manual dispatch.

### What it verifies
- Web syntax checks (`node --check`) for the JavaScript application files.
- Windows-targeted desktop host build for `desktop-host/MoatHouseHandover.Host.csproj`.
- Local package publish output under `dist/local-host`.
- Packaged asset presence checks for:
  - `webapp/index.html`
  - `config/runtime.config.json`
- Upload of packaged output as a CI artifact when packaging succeeds.

### What it does **not** fully verify
- Outlook COM draft creation behavior on an actual user workstation.
- ACE/OLEDB runtime installation and behavior on the target workstation.
- Interactive WebView2 UI behavior during real user workflows.
- Real shared-folder/network permission behavior in workplace environments.

Windows CI is build/package validation, not a replacement for real workstation runtime verification.
