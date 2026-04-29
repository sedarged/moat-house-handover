# Testing and Verification Guide

This guide defines the evidence commands agents should use before claiming work is ready.

For business acceptance criteria, always read `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md` first.

## Verification levels

Use precise language in reports.

### Static / syntax verified

The relevant scripts parsed files and did not report syntax errors.

### Browser/mock verified

Playwright opened the web UI in a browser using the mock host bridge and seeded data.

Browser/mock verification is useful for UI smoke checks and screenshots, but it is not full Windows runtime verification.

### Build/package verified

The host build and local package scripts completed successfully.

### Windows runtime verified

The app was actually launched and exercised on a Windows workstation with the real WPF/WebView2 host and real runtime dependencies.

Only claim this when it actually happened.

## Core repository checks

For most implementation work, run the strongest relevant subset of:

```bash
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If `.NET` is missing and the helper exists, run:

```bash
bash scripts/bootstrap-dotnet.sh
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

## Web/UI evidence checks

The repo includes a browser/mock UI harness for Codex/CI evidence.

Install dependencies:

```bash
npm install
npx playwright install --with-deps chromium
```

Then run:

```bash
npm run check:web
npm run test:ui
npm run screenshots
npm run verify:screenshots
```

These commands use:

- `playwright.config.mjs`
- `scripts/serve-web-dev.mjs`
- `tests/ui/helpers/mockHostBridge.mjs`
- `tests/ui/*.spec.js`
- `scripts/capture-screenshots.mjs`
- `scripts/verify-screenshots.mjs`

## Screenshot evidence

Generated browser/mock screenshots are expected under:

```text
test-evidence/screenshots/01-shift-screen.png
test-evidence/screenshots/02-dashboard.png
test-evidence/screenshots/05-budget.png
test-evidence/screenshots/06-preview.png
```

`scripts/verify-screenshots.mjs` checks that these files exist, are PNG files, have reasonable dimensions, and are not tiny blank files.

It does not judge visual quality. Human review is still required for final design judgment.

## GitHub Actions

`UI Evidence` runs on pull requests affecting the web app, UI tests, screenshot scripts, Playwright config, or the UI evidence workflow.

It runs:

```bash
npm install
npx playwright install --with-deps chromium
npm run check:web
npm run test:ui
npm run screenshots
npm run verify:screenshots
```

It uploads:

- `playwright-report/`
- `test-results/`
- `test-evidence/screenshots/`

as workflow artifacts.

## Windows runtime checks

The following cannot be fully proven by browser/mock tests:

- WPF host launch
- WebView2 runtime behaviour inside the desktop shell
- Access/ACE/OLEDB behaviour
- real Windows filesystem/shared folder permissions
- Explorer folder opening
- Outlook COM draft creation
- packaged app execution on a workstation

If these are not tested on Windows, say so.

## Recommended verification by change type

### Docs-only change

Usually enough:

```bash
# no application code changed
```

Still check links/paths manually where possible.

### Web UI or JavaScript screen change

Run:

```bash
npm run check:web
npm run test:ui
npm run screenshots
npm run verify:screenshots
```

If the change can affect packaged assets, also run:

```bash
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

### Desktop host / C# / bridge change

Run:

```bash
bash scripts/check-prereqs.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If the change touches web assets or bridge payloads, also run UI evidence checks.

### Budget / Preview / Report contract change

Run both backend/package and UI evidence checks.

Also inspect payload names between:

- `desktop-host/src/*Contracts.cs`
- `desktop-host/src/*Service.cs`
- `webapp/js/services/*.js`
- `webapp/js/screens/*.js`

## Failure reporting

Do not hide failures.

If a command fails, report:

```text
command: FAIL — exact failing command and short reason
```

If a command cannot run in the environment, report:

```text
command: BLOCKED — exact blocker
```

Do not convert a failed or blocked check into a passing claim.

## Final report checklist

Every non-trivial implementation report should include:

- changed files
- commands run
- PASS/FAIL/BLOCKED status for each command
- screenshot paths if UI changed
- browser/mock vs Windows runtime distinction
- remaining risks
- exact final status
