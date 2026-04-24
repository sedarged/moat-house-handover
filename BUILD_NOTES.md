# Build Notes (Stage 2A)

This repository now contains **Stage 2A infrastructure** for MOAT HOUSE HANDOVER v2.

## What exists now (implemented)
- WPF WebView2 host startup initializer
- Runtime config loading from real JSON file
- Safe config path lookup order for workstation deployment
- Required runtime key validation (`accessDatabasePath`, `attachmentsRoot`, `reportsOutputRoot`)
- Access bootstrap service that creates DB/schema/seed idempotently
- Required folder creation for attachments and reports roots
- Bootstrap logging to local files
- Local packaged web asset resolution (`<app>/webapp`) with dev fallback
- Host ↔ web bridge skeleton (`runtime.getStatus`, `shell.openOutputFolder`)

## Runtime config file
Default packaged config location:
- `<app>/config/runtime.config.json`

Alternative lookup locations are documented in:
- `docs/STAGE2A_CONTINUATION.md`

## Stage boundary (explicit)
### Implemented in Stage 2A only
- Startup/runtime infrastructure and bootstrap path

### Deferred to Stage 2B+
- DAO repositories and service persistence logic
- Full session open/create/clear behavior
- Department, budget, attachment, preview, report and send workflows

No full business workflow is implemented in Stage 2A.
