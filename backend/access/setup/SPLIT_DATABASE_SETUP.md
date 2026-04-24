# Split Access Setup

## Objective
Use Access split design:
- Back-end (`*_be.accdb`) on shared location
- Front-end logic in local app (WebView2 host + service layer)

## Artifacts
- Schema source: `../schema/001_tables_and_indexes.sql`
- Seed source: `../seed/001_seed_lookup_data.sql`

## Stage 2A implementation
- Host startup executes Access bootstrap automatically.
- If Access file is missing, host creates `.accdb` file.
- Required tables/indexes are created only when missing.
- Approved lookup seed records are inserted only when missing.
- Bootstrap is idempotent and safe to run on every startup.

## Deployment constraints
1. End users run local desktop app package.
2. Shared folders must be accessible for:
   - Access database file path
   - Attachments root
   - Reports output root
3. Attachments are file copies; Access stores only metadata + paths.

## Stage 2B follow-up
- Add DAO repository layer against bootstrapped Access backend.
- Add migration/version stamp strategy for future schema upgrades.
