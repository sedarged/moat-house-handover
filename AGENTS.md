# AGENTS.md — MOAT HOUSE HANDOVER v2

This file defines how Codex and other coding agents must work in this repository.

The goal is simple: protect the project architecture, avoid fake progress, make changes safely, and always report verification honestly.

---

## 1. Project goal

Maintain and extend **MOAT HOUSE HANDOVER v2** as a **local-first Windows desktop handover application** while preserving the existing business workflow and stage plan from the spec set.

This is an operational handover tool, not a demo app. Reliability, clear user-facing errors, repeatable packaging, and honest Windows runtime validation are more important than cosmetic changes.

---

## 2. Read-first rule — source of truth

Before planning or editing code, read these files first and treat them as authoritative:

1. `01_MASTER_SPEC.md`
2. `02_SCREEN_BLUEPRINT.md`
3. `03_DATA_MODEL.md`
4. `04_SERVICE_MAP.md`
5. `05_BUILD_ORDER.md`
6. `06_ACCEPTANCE_TESTS.md`
7. `07_CLAUDE_CODE_PROMPTS.md`

Then read the current implementation context:

- `README.md`
- `BUILD_NOTES.md`
- `docs/*`
- `desktop-host/*`
- `webapp/*`
- `backend/access/*`
- `scripts/*`
- `.github/workflows/*`

Do not make architectural decisions until these have been inspected.

---

## 3. Locked architecture — must not drift

Keep this architecture unchanged unless explicitly directed by the human maintainer:

- Local-first Windows desktop application
- WPF desktop host
- WebView2 UI surface
- HTML/CSS/JS frontend assets
- Access-oriented backend using DAO/Access SQL path where applicable
- Attachments and reports stored as files/folders, not embedded database binaries
- End-user workflow must not require terminal usage on work machines
- Packaging must support a practical Windows workstation deployment path

Do **not** replace this with:

- cloud-first architecture
- web server architecture
- mobile app architecture
- Electron rewrite
- random framework migration
- hosted database dependency
- terminal-dependent user workflow

---

## 4. Non-negotiable agent rules

- Inspect before editing.
- Do not guess when files, scripts, or tests can be inspected.
- Do not rewrite the project from scratch.
- Do not remove features just to make checks pass.
- Do not invent or implement future business stages early.
- Do not introduce unrelated frameworks or tooling.
- Do not claim features are implemented if they are scaffold-only.
- Do not claim build, package, runtime, or workflow success unless verified.
- Do not hide failures or environment limits.
- Do not commit secrets, credentials, tokens, private machine data, or personal paths as real defaults.
- Do not break Windows runtime behavior while fixing Linux/cloud/static checks.
- Prefer minimal, durable, reviewable changes over broad rewrites.

---

## 5. Stage discipline

Follow `05_BUILD_ORDER.md` stage boundaries.

When working on a task:

1. Identify the requested stage or scope.
2. Confirm which spec sections apply.
3. Implement only the requested scope.
4. Explicitly label deferred work.
5. Do not silently extend scope into future stages.

If a requested change conflicts with the stage plan or source-of-truth specs, report the conflict before changing architecture.

---

## 6. Repository boundaries

Use the repository structure intentionally:

- `desktop-host/` — Windows host runtime, WPF shell, WebView2 bootstrap, host bridge, filesystem/runtime integration.
- `webapp/` — HTML/CSS/JS UI routes, screen logic, client-side state, bridge calls.
- `backend/access/` — Access schema, seed data, setup/bootstrap artefacts, persistence model.
- `docs/` — stage notes, continuation notes, operator guidance, developer guidance, runtime checklist.
- `scripts/` — repeatable helper scripts for prereq checks, web checks, build, package, and package verification.
- `.github/workflows/` — CI validation, especially Windows build/package checks.

Do not move responsibilities across these boundaries without a clear reason and matching documentation update.

---

## 7. Required task lifecycle

For every non-trivial task, follow this lifecycle:

### A. Orient

Run or inspect equivalent:

```bash
git status
git branch --show-current
git log --oneline -n 10
```

Inspect the relevant files before editing.

### B. Plan

Create a short internal plan based on the inspected files.

The plan must identify:

- scope
- affected files
- verification commands
- known environment limits

### C. Change

Make focused changes only.

Prefer existing patterns in the repository. Do not introduce new abstractions unless they reduce real duplication or fix a real maintainability issue.

### D. Verify

Run the strongest relevant checks available in the environment.

### E. Report

End with a clear summary of:

- what changed
- why it changed
- commands run
- pass/fail results
- remaining risks
- manual Windows checks still required

---

## 8. Preferred verification commands

Prefer existing helper scripts over ad hoc commands.

When relevant, run:

```bash
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If `.NET` is missing and the repository provides a bootstrap helper, attempt:

```bash
bash scripts/bootstrap-dotnet.sh
bash scripts/check-prereqs.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

For C# work:

```bash
dotnet restore
dotnet build --configuration Release
```

If test projects exist:

```bash
dotnet test
```

For JavaScript work:

- run the repository web check script first
- if `package.json` exists, inspect available scripts before running commands
- run only scripts that actually exist, such as `npm test`, `npm run lint`, or `npm run build`

Do not invent unavailable commands.

---

## 9. Verification honesty rules

Every final response must separate these categories:

1. **Verified** — command or runtime check was executed and passed.
2. **Partially verified** — static/build/package checks passed, but real Windows runtime still needs manual validation.
3. **Not verified** — could not be tested in this environment, with exact reason.

Never write vague statements such as:

- "should work"
- "looks good"
- "probably fixed"
- "verified" without command evidence

Use evidence instead:

- command run
- result
- relevant output summary
- remaining limitation if any

---

## 10. Windows runtime constraints

The real runtime target is Windows with:

- WebView2 runtime
- Access Database Engine / ACE OLEDB behavior where applicable
- Windows filesystem behavior
- possible Outlook COM draft workflow
- local/shared workplace folders

Cloud, Linux, and container environments are for code editing and partial checks only.

The following usually require real Windows validation:

- WebView2 interactive UI launch
- Access/ACE/OLEDB provider behavior
- Outlook COM draft creation
- Windows Explorer folder opening
- local/shared folder permissions
- packaged app execution on a workstation
- full end-to-end operator workflow

If these were not actually tested on Windows, mark them as **Partially verified** or **Not verified**, not fully verified.

---

## 11. Build, package, and CI expectations

When changing build, package, scripts, or CI:

- keep local scripts and GitHub Actions aligned
- ensure package verification checks real required assets
- ensure scripts fail clearly when prerequisites are missing
- avoid false-positive success paths
- keep package output practical for Windows workstation use
- update `BUILD_NOTES.md` and docs if commands or outputs change

Inspect:

- `scripts/*`
- `.github/workflows/*`
- `BUILD_NOTES.md`
- `docs/LOCAL_WINDOWS_RUNBOOK.md`
- `docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md`

---

## 12. WebView2 and host bridge rules

When changing `desktop-host/` or bridge behavior:

- preserve safe WebView2 initialization
- preserve local asset loading
- validate bridge message routing
- handle malformed messages gracefully
- keep async flows safe
- return useful user-facing errors
- avoid silent failures during bootstrap
- do not assume Windows-only dependencies exist in cloud checks

Bridge failures must be visible and diagnosable.

---

## 13. Access backend and persistence rules

When changing Access/backend logic:

- protect existing data
- keep bootstrap/setup idempotent
- avoid destructive migrations unless explicitly required
- handle missing/locked database clearly
- handle missing ACE/OLEDB provider clearly
- keep attachment binaries out of the database unless specs explicitly change
- preserve auditability where required
- keep schema/seed changes aligned with `03_DATA_MODEL.md`

Persistence changes must be documented and verifiable.

---

## 14. Reports and send workflow rules

When changing report or send-related behavior:

- preserve draft-only boundaries where specified
- do not implement unsafe real email sending unless explicitly required by specs
- validate before report/send package creation
- return clear errors when Outlook or filesystem operations are unavailable
- ensure generated reports are included in package checks where applicable
- update runtime checklist if manual validation steps change

---

## 15. Documentation rules

Documentation must match implementation reality.

Update docs when:

- commands change
- scripts change
- package output changes
- runtime requirements change
- manual test steps change
- a feature is implemented, deferred, or removed

Do not document scaffold-only features as complete.

---

## 16. Security and safety rules

Always check for:

- committed secrets
- hardcoded credentials
- unsafe shell execution
- path traversal risk
- arbitrary file overwrite risk
- unsafe attachment handling
- accidental real email sending
- logs exposing sensitive local information

Use safe defaults and clear validation.

---

## 17. PR and commit discipline

If working through a pull request:

- keep commits focused
- use clear commit messages
- explain why each file changed
- do not mix unrelated refactors with bug fixes
- do not mark a PR ready if required checks are failing without explanation

Recommended commit message style:

```text
Area: concise change summary
```

Examples:

```text
Scripts: tighten package asset verification
Host: add safe WebView2 startup diagnostics
Docs: update Windows runtime checklist
```

---

## 18. Final report format for agents

End substantial tasks with this structure:

```text
## Summary
- What was inspected
- What was changed
- Current readiness status

## Files changed
- path: what changed and why

## Verification
- command: PASS/FAIL/BLOCKED — evidence summary

## Windows runtime status
- Verified:
- Partially verified:
- Not verified:

## Remaining risks
- Real remaining risks only

## Next action
- One clear recommended next step
```

Do not bury failures. Put blockers and unverified runtime areas in the final report.

---

## 19. Definition of done

A task is done only when:

- relevant source-of-truth specs were inspected
- implementation changes are complete for the requested scope
- relevant checks were run or honestly marked blocked
- docs were updated if behavior changed
- no known critical/high issue introduced by the change remains unresolved
- Windows-only limitations are clearly identified
- final report includes evidence, not vague confidence

---

## 20. Default agent priority order

When multiple issues are found, fix in this order:

1. Build-breaking issues
2. Runtime startup issues
3. Data loss or unsafe persistence risks
4. Broken core user workflows
5. Packaging or CI false positives
6. Diagnostics and user-facing error quality
7. Documentation mismatches
8. Maintainability improvements
9. Cosmetic cleanup

Do not spend time on cosmetics while critical verification or workflow issues remain.
