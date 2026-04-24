# MOAT HOUSE HANDOVER v2 — CLAUDE CODE PROMPTS

## Prompt 1 — Project Bootstrap
You are building MOAT HOUSE HANDOVER v2 as a local Windows desktop app.

Read these files first and treat them as the only source of truth:
- 01_MASTER_SPEC.md
- 02_SCREEN_BLUEPRINT.md
- 03_DATA_MODEL.md
- 04_SERVICE_MAP.md
- 05_BUILD_ORDER.md
- 06_ACCEPTANCE_TESTS.md

Requirements:
- Build locally first.
- UI must be HTML/CSS/JS.
- Host must be a local Windows shell using WebView2.
- Backend must be Access split database.
- Do not invent major features beyond the spec.
- Keep the original business workflow intact.

Task:
Create the initial repository structure, local shell app structure, static web app structure, and a short BUILD_NOTES.md explaining how to run the project locally.

Output:
- file tree
- created files
- short explanation of architecture choices
- any blockers

## Prompt 2 — Access Backend Schema
Read:
- 01_MASTER_SPEC.md
- 03_DATA_MODEL.md
- 05_BUILD_ORDER.md

Task:
Create the Access backend schema definition for all tables, indexes, and seed data required for v2.

Requirements:
- preserve original business rules
- include audit/config tables
- include notes about split database deployment
- do not store attachment binaries in the DB

Output:
- schema scripts or builder code
- seed data definition
- verification checklist

## Prompt 3 — Service Layer
Read:
- 01_MASTER_SPEC.md
- 03_DATA_MODEL.md
- 04_SERVICE_MAP.md

Task:
Implement the service/repository layer for:
- sessions
- dashboard
- departments
- attachments
- budget

Requirements:
- use DAO + Access SQL
- keep SQL encapsulated
- prefer clear repository/service separation
- include comments and acceptance notes

Output:
- repository files
- service files
- examples of payload structures returned to UI

## Prompt 4 — Shift + Dashboard UI
Read:
- 01_MASTER_SPEC.md
- 02_SCREEN_BLUEPRINT.md
- 04_SERVICE_MAP.md
- 06_ACCEPTANCE_TESTS.md

Task:
Build the Shift Screen and Dashboard Screen.

Requirements:
- app-like layout
- proper shift/date open flow
- blank new-day prompt logic
- dashboard tiles driven by real backend data
- no fake placeholders once data wiring exists

Output:
- UI files
- wiring code
- test steps

## Prompt 5 — Department + Attachments
Read:
- 01_MASTER_SPEC.md
- 02_SCREEN_BLUEPRINT.md
- 03_DATA_MODEL.md
- 04_SERVICE_MAP.md
- 06_ACCEPTANCE_TESTS.md

Task:
Build the Department Screen and Attachments flow.

Requirements:
- metric/non-metric rules must match the spec
- support add/remove/prev/next/view full
- no stale preview state
- use managed file storage paths
- metadata in DB only

Output:
- UI files
- service wiring
- file handling logic
- acceptance checklist for attachment behavior

## Prompt 6 — Budget + Preview
Read:
- 01_MASTER_SPEC.md
- 02_SCREEN_BLUEPRINT.md
- 04_SERVICE_MAP.md
- 06_ACCEPTANCE_TESTS.md

Task:
Build the Budget Screen and Preview Screen.

Requirements:
- real save/load
- variance calculation
- preview built from real payloads

Output:
- code changes
- test steps
- known limitations if any

## Prompt 7 — Reports + Send
Read:
- 01_MASTER_SPEC.md
- 02_SCREEN_BLUEPRINT.md
- 04_SERVICE_MAP.md
- 06_ACCEPTANCE_TESTS.md

Task:
Build report generation and send workflow.

Requirements:
- generate handover and budget outputs
- support shift-based email profiles
- create draft payload and local draft workflow

Output:
- code changes
- output file rules
- test checklist
