# MOAT HOUSE HANDOVER v2

Stage 2A baseline for a local-first Windows desktop handover application.

## Repository Structure

- `desktop-host/` — WPF + WebView2 desktop host with runtime bootstrap
- `webapp/` — HTML/CSS/JS application shell and placeholder screens
- `backend/access/` — Access schema and seed artifacts used by host bootstrap
- `docs/` — stage notes and continuation guidance
- `BUILD_NOTES.md` — local build and runtime notes

## Stage 2A Scope Completed
- Local runtime config loading from JSON with safe path fallback strategy
- Startup validation for required runtime config values
- Startup initialization service (folder checks + Access bootstrap + logging)
- Idempotent Access schema + approved seed bootstrap path
- Deployment-safe local web asset resolution without local web server
- Initial host-to-web bridge contract skeleton (`runtime.getStatus`, `shell.openOutputFolder`)

## Still Deferred (by design)
- DAO repository implementation
- Production business save/load workflows
- Attachment/report/send end-to-end runtime workflows
