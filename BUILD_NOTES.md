# Build Notes — MOAT HOUSE HANDOVER v2

These notes describe the current build, package, and runtime validation flow.

The active product source of truth is:

- `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`
- `AGENTS.md`
- `MASTER_TASK_DEEP_REVIEW_UI_UX.md` for deep review / correction work

Older fragmented stage/spec files have been retired.

## Current architecture

- WPF desktop host
- WebView2 UI surface
- HTML/CSS/JS frontend assets
- Access-oriented backend/bootstrap path
- Runtime config loaded from JSON
- Attachments stored as managed files/folders
- Reports stored as managed files/folders
- Outlook draft-only workflow

## Runtime config file

Default packaged config location:

- `<app>/config/runtime.config.json`

Typical lookup order used by the host runtime:

1. `MOAT_HANDOVER_CONFIG` environment variable
2. packaged app config path
3. approved workstation/shared fallback paths where configured
4. development fallback paths during local development

Required runtime keys include:

- `accessDatabasePath`
- `attachmentsRoot`
- `reportsOutputRoot`

## Important runtime dependencies

- `Microsoft.Web.WebView2` for the desktop WebView host
- `System.Data.OleDb` / ACE OLEDB path for Access-oriented runtime/database access where applicable
- Windows filesystem permissions for config, attachments, reports, and logs
- Outlook COM availability only for draft creation on a real Windows workstation

## Helper scripts

Run from repository root.

```bash
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If `.NET` is missing in a cloud/dev environment:

```bash
bash scripts/bootstrap-dotnet.sh
bash scripts/check-prereqs.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

## What local/CI checks verify

- JavaScript syntax/static checks
- Windows-targeted host build
- local package publish output
- required packaged assets such as `webapp/index.html` and `config/runtime.config.json`

## What local/CI checks do not fully verify

These require a real Windows workstation/runtime test:

- interactive WPF/WebView2 user workflow
- Access/ACE OLEDB installed runtime behaviour
- Outlook COM draft creation
- shared folder/network permissions
- packaged app execution by real users

Do not claim full Windows runtime verification unless these have been tested on a real Windows workstation.
