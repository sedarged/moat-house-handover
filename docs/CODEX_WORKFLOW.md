# Codex Workflow Guide

This guide defines how Codex Web should be used in this repository.

It complements `AGENTS.md`, `docs/PROJECT_OVERVIEW.md`, and `docs/TESTING.md`.

## Default mode choice

Use Codex Ask mode first when the task is broad, unclear, or risky.

Use Codex Code mode only when the task has:

- clear scope
- files or areas to inspect
- acceptance criteria
- verification commands
- expected PR output

## First Codex task after setup

Before asking Codex to implement feature work, run a read-only audit task:

```text
ASK MODE — Do not modify files.

Read:
- AGENTS.md
- HANDOVER_APP_V2_SOURCE_OF_TRUTH.md
- MASTER_TASK_DEEP_REVIEW_UI_UX.md
- docs/PROJECT_OVERVIEW.md
- docs/TESTING.md
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

The goal is to confirm Codex understands the repository before it edits code.

## Code mode task shape

Code tasks should look like GitHub issues, not huge open-ended prompts.

Recommended structure:

```text
MODE: CODE

Repository:
https://github.com/sedarged/moat-house-handover

Title:
<one clear phase/task>

Context:
<what is already merged and why this task exists>

Read first:
- AGENTS.md
- HANDOVER_APP_V2_SOURCE_OF_TRUTH.md
- docs/PROJECT_OVERVIEW.md
- docs/TESTING.md

Scope:
<small, bounded scope>

Files to inspect:
<exact file list or directories>

Acceptance criteria:
<observable requirements>

Verification:
<commands that must be run>

PR requirements:
<branch, commit, PR body, evidence>

Final response:
<required summary and final status>
```

## Task size rule

Prefer small phase PRs.

One task should normally change one area:

- one screen
- one service/contract path
- one report path
- one security cleanup
- one documentation/tooling improvement

Avoid a single task that mixes:

- backend schema
- C# services
- web UI
- report generation
- security audit
- design rewrite
- screenshots
- docs

unless the maintainer explicitly asks for a broad final pass.

## Required setup commands

For UI/browser evidence, Codex should be able to run:

```bash
npm install
npx playwright install --with-deps chromium
npm run check:web
npm run test:ui
npm run screenshots
npm run verify:screenshots
```

For host/package checks, Codex should run the relevant subset of:

```bash
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If `.NET` is missing and the helper exists:

```bash
bash scripts/bootstrap-dotnet.sh
```

## Branch and PR rules

Use descriptive branch names:

```text
codex/<phase-or-task-name>
fix/<specific-fix>
docs/<documentation-task>
infra/<tooling-task>
```

Every Code mode implementation should create a PR unless explicitly told not to.

PR body must include:

- what changed
- what did not change
- verification commands and results
- screenshot evidence or exact screenshot blocker
- Windows-only limitations
- final status

## Evidence rules

Codex must not claim completion without evidence.

Evidence can be:

- changed file list
- commit SHA
- PR link
- command output summary
- screenshot paths
- workflow artifacts
- explicit blocker

Words alone are not evidence.

## UI task rules

For UI work, Codex must:

- run `npm run check:web`
- run `npm run test:ui`
- run `npm run screenshots`
- run `npm run verify:screenshots`
- clearly label screenshots as browser/mock unless they were taken from real WPF/WebView2

Browser/mock screenshots do not prove Windows runtime.

## Runtime honesty

Codex must keep these categories separate:

- Static/syntax verified
- Browser/mock verified
- Build/package verified
- Windows runtime verified
- Not verified
- Blocked

Do not claim Windows runtime verification unless a real Windows workstation runtime was used.

## Internet access

Do not rely on general internet browsing for normal implementation work.

The repository should contain enough instructions, scripts, fixtures, and tests for Codex to work without internet during the coding phase.

Use external research only when the task explicitly requires current official documentation or unknown third-party behaviour.

## Good next Codex sequence

Recommended order after this documentation PR:

1. Ask-mode repo audit.
2. Small PR: PR quality gate / unsafe `innerHTML` checker.
3. Small PR: UI design system documentation.
4. Feature PR: Budget UI final polish with screenshot evidence.
5. Feature PR: Preview Budget integration with screenshot evidence.
6. Feature PR: Budget report integration.
7. Security PR: broader unsafe rendering cleanup.

## Stop conditions

Codex should stop and report `NO CODE CHANGES MADE — BLOCKED` when:

- required files are missing and cannot be inferred safely
- environment lacks required tools and setup cannot install them
- PR metadata cannot be created/updated
- tests fail and the fix is outside requested scope
- task scope is too broad to complete safely

A blocker is acceptable. Fake progress is not.
