# MOAT HOUSE HANDOVER v2

Stage 2D Extended baseline for a local-first Windows desktop handover application.

## Repository Structure

- `desktop-host/` — WPF + WebView2 desktop host with runtime bootstrap
- `webapp/` — HTML/CSS/JS application shell and placeholder screens
- `backend/access/` — Access schema and seed artifacts used by host bootstrap
- `docs/` — stage notes and continuation guidance
- `scripts/` — repeatable helper scripts for checks, build, SDK bootstrap, and local packaging
- `BUILD_NOTES.md` — local build and runtime notes

## Stage 2D Extended Scope Completed
- Local runtime config loading from JSON with safe path fallback strategy
- Startup validation for required runtime config values
- Startup initialization service (folder checks + Access bootstrap + logging)
- Idempotent Access schema + approved seed bootstrap path
- Deployment-safe local web asset resolution without local web server
- Session + department persistence (Stage 2B/2C)
- Attachment bridge actions and Access-backed metadata persistence (`attachment.list`, `attachment.add`, `attachment.remove`, `attachment.openViewer`)
- Host-side local file picker/copy flow (`file.pickFile`)
- Department attachment panel + viewer flow + dashboard attachment counts

## Still Deferred (by design)
- Budget persistence workflows
- Preview/report/send end-to-end runtime workflows
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
