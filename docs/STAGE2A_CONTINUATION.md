# Stage 2A Continuation Notes

## Implemented in Stage 2A

### Desktop runtime configuration
- Host now loads runtime config from a real JSON file.
- Config lookup order:
  1. `MOAT_HANDOVER_CONFIG` environment variable
  2. `<app>/config/runtime.config.json`
  3. `%ProgramData%/MoatHouseHandover/config/runtime.config.json`
  4. `%LocalAppData%/MoatHouseHandover/config/runtime.config.json`
  5. Development fallback `desktop-host/config/runtime.config.json`
- Required key validation is enforced for:
  - `accessDatabasePath`
  - `attachmentsRoot`
  - `reportsOutputRoot`
- Startup fails fast with a clear error when config cannot be loaded or validated.

### Safer web asset runtime resolution
- Host no longer assumes a repository relative path only.
- Primary runtime path is packaged output: `<app>/webapp/index.html`.
- Development fallback remains available for local developer runs.
- No local web server is required; assets are loaded directly as local files.

### Executable Access bootstrap/setup
- Host startup checks if Access backend file exists.
- If missing, it creates the `.accdb` database using ADOX.
- Schema and index creation is idempotent through existence checks.
- Approved seed data from Stage 1 artifacts is inserted only when missing.
- Existing tables/indexes/seeds are preserved and do not break startup.

### Host-layer backend initialization service
- Startup initializer now performs:
  - config load
  - required folder checks/creation
  - database existence + bootstrap
  - asset resolution
  - bootstrap logging
- Bootstrap logs are written to configured log root (or local fallback).

### Host ↔ web bridge skeleton
- Added a message contract skeleton with request/response envelope.
- Bridge JSON serialization now explicitly uses camelCase response payloads and case-insensitive request parsing.
- Implemented bridge handlers:
  - `runtime.getStatus`
  - `shell.openOutputFolder`
  - placeholder `file.pickFile` (explicit Stage 2A not implemented)
- Added web bridge helper to request runtime status.

## Explicitly deferred from Stage 2A
- DAO repository implementation
- Session/department/budget save-load business workflow
- Attachment add/remove runtime workflow
- Report generation runtime workflow
- Email draft/send runtime workflow
- Full bridge API surface for file dialogs and business operations

## Stage 2B next steps
1. Implement DAO connection abstraction and repository base patterns.
2. Add Session service (`openSession`, create blank day, clear day).
3. Add Dashboard payload assembly from real Access data.
4. Expand host bridge contract to support service calls safely.
5. Add standardized result/error envelopes for service responses.
6. Add initial integration tests for startup + session open path.
