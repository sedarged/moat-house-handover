# AI Task Queue — MOAT HOUSE HANDOVER v2

This queue gives Codex and other coding agents a safe order of work.

It is not a replacement for `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`. The source-of-truth file always wins for product behaviour.

## Operating rule

Do not ask an agent to "finish the whole app" in one PR.

Use small evidence-backed PRs:

- one clear scope
- one branch
- one PR
- one verification report
- screenshot evidence for UI work
- honest Windows runtime limitations

## Required first Codex step

Before feature work, run Codex in Ask mode:

```text
ASK MODE — Do not modify files.

Read:
- AGENTS.md
- HANDOVER_APP_V2_SOURCE_OF_TRUTH.md
- MASTER_TASK_DEEP_REVIEW_UI_UX.md
- docs/PROJECT_OVERVIEW.md
- docs/TESTING.md
- docs/CODEX_WORKFLOW.md
- docs/AI_TASK_QUEUE.md

Then explain:
1. Current architecture
2. What is already implemented
3. What is missing
4. Which files are risky
5. Which tests protect the app
6. What should be the next safest PR

Do not create a branch or PR.
```

Only move to Code mode after this audit looks grounded.

## Recommended PR sequence

### PR A — PR quality and unsafe rendering gates

Goal:
Add simple repository checks that stop misleading PR metadata and new unsafe UI rendering.

Suggested files:

- `.github/pull_request_template.md`
- `scripts/check-unsafe-innerhtml.mjs`
- `scripts/validate-pr-body.mjs`
- `.github/workflows/pr-quality.yml`

Acceptance criteria:

- New `innerHTML =` assignments in screen files fail unless explicitly allowlisted.
- PR body includes phase/scope, verification, screenshot evidence or blocker, and Windows-only limitations.
- PR body must not claim full app completion or Windows runtime verification without evidence.

Verification:

```bash
npm run check:web
node scripts/check-unsafe-innerhtml.mjs
node scripts/validate-pr-body.mjs --help
```

### PR B — UI design system documentation

Goal:
Document the current modern Moat House UI style so Codex does not guess what "app-like" means.

Suggested files:

- `docs/design/UI_DESIGN_SYSTEM.md`
- `docs/design/SCREEN_MANIFEST.md`
- `docs/design/screen-dashboard.md`
- `docs/design/screen-budget.md`
- `docs/design/screen-preview.md`

Acceptance criteria:

- Defines dark/orange design language.
- Defines header, infobar, cards, summary tiles, tables, buttons, badges, forms, validation, spacing, and empty states.
- Explains what not to do: no legacy/VBA copying, no plain spreadsheet dumps, no huge empty gaps.

Verification:

Docs-only; inspect for accuracy against current app and screenshots.

### PR C — Budget UI final polish with evidence

Goal:
Use current Budget foundation and UI evidence tooling to make Budget View visibly complete and app-like.

Scope:

- `webapp/js/screens/BudgetScreen.js`
- `webapp/css/app.css`
- relevant UI tests

Acceptance criteria:

- Budget screen shows all required budget summary fields.
- Labour rows are visible, editable, and internally scroll where needed.
- No huge bottom gap.
- Browser/mock screenshot `test-evidence/screenshots/05-budget.png` is generated and shows realistic populated data.

Verification:

```bash
npm run check:web
npm run test:ui
npm run screenshots
npm run verify:screenshots
```

### PR D — Preview Budget integration

Goal:
Preview must show saved Budget Summary and Budget Rows read-only.

Scope:

- `webapp/js/screens/PreviewScreen.js`
- `webapp/js/services/previewService.js`
- relevant host contracts/services if needed
- UI tests and screenshots

Acceptance criteria:

- Preview shows Budget Summary.
- Preview shows granular Budget Rows.
- Preview is read-only.
- Missing values display as `—`.
- Browser/mock screenshot `test-evidence/screenshots/06-preview.png` proves it.

Verification:

```bash
npm run check:web
npm run test:ui
npm run screenshots
npm run verify:screenshots
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

### PR E — Budget report integration

Goal:
Generated Budget report includes the full budget information from source of truth.

Scope:

- report contracts/services
- report HTML/output generation
- service bridge payloads
- tests or fixtures if available

Acceptance criteria:

- Report contains shift/date/session header.
- Report contains lines planned, rows, planned staff, staff used, reasons, totals, register total, holiday/absent/other/agency counts, variance, comments, generated at/by.
- User-entered comments/reasons are escaped.
- Report does not send email.

Verification:

```bash
bash scripts/check-prereqs.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
npm run check:web
```

### PR F — Broader security cleanup

Goal:
Clean unsafe rendering and file/path handling risks discovered after UI and report work.

Scope:

- unsafe `innerHTML`
- path traversal protections
- report/attachment path handling
- user/persisted text escaping

Acceptance criteria:

- No new unsafe dynamic `innerHTML`.
- User/persisted values use `textContent`, `.value`, DOM creation, or explicit escaping.
- Security checker passes.

Verification:

```bash
npm run check:web
npm run test:ui
npm run screenshots
npm run verify:screenshots
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

### PR G — Windows runtime checklist / manual validation docs

Goal:
Create a repeatable checklist for real workstation validation.

Suggested files:

- `docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md`
- `docs/LOCAL_WINDOWS_RUNBOOK.md`

Acceptance criteria:

- Describes how to validate WPF/WebView2 launch.
- Describes Access/ACE checks.
- Describes Outlook draft-only checks.
- Describes filesystem/shared-folder checks.
- Clearly separates browser/mock evidence from Windows runtime evidence.

## Task anti-patterns

Do not use these prompts:

```text
finish everything
make it perfect
fix all issues
full final pass
make UI nice
```

Use bounded task prompts instead:

```text
Implement Budget UI final polish only, using existing design system and UI evidence tooling.
```

## Definition of ready for Codex Code mode

A task is ready for Code mode only when it has:

- clear scope
- files to inspect
- non-goals
- acceptance criteria
- verification commands
- PR requirements
- final response format

If any of these are missing, use Ask mode first.
