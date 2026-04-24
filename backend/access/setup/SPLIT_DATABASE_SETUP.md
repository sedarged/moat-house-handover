# Split Access Setup (Design Guidance)

## Objective
Use Access split design:
- Back-end (`*_be.accdb`) on shared location
- Front-end logic in local app (WebView2 host + service layer)

## Stage 1 setup artifacts
- Schema script: `../schema/001_tables_and_indexes.sql`
- Seed script: `../seed/001_seed_lookup_data.sql`

## Deployment constraints
1. End users run local desktop app package.
2. Shared folders must be accessible for:
   - Access database file path
   - Attachments root
   - Reports output root
3. Attachments are file copies; Access stores only metadata + paths.

## Stage 2 implementation checklist
- Translate SQL draft into executable Access DAO setup routine
- Add idempotency checks for table/index creation
- Add seed upsert behavior
- Validate front-end local config path resolution
