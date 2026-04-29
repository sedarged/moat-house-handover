# AGENTS.md — MOAT HOUSE HANDOVER v2

This file defines how Codex, Claude Code, and other coding agents must work in this repository.

The goal is simple: protect the project architecture, avoid fake progress, make changes safely, and always report verification honestly.

---

## 1. Active source of truth

Before planning or editing code, read these files first and treat them as authoritative:

1. `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`
2. `MASTER_TASK_DEEP_REVIEW_UI_UX.md` when doing review/correction work
3. `README.md`
4. `BUILD_NOTES.md`
5. `docs/*` when runtime/deployment/manual testing details are relevant

The old fragmented plan/spec files have been retired. Do not use deleted `01_*` to `07_*` documents as source of truth.

If any older note, PR comment, screenshot, or generated artifact conflicts with `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`, the source-of-truth file wins.

---

## 2. Project goal

Maintain and extend **MOAT HOUSE HANDOVER v2** as a **local-first Windows desktop handover application**.

This is an operational handover tool, not a demo app. Reliability, clear user-facing errors, repeatable packaging, realistic screenshot evidence, and honest Windows runtime validation are more important than cosmetic claims.

---

## 3. Locked architecture

Keep this architecture unchanged unless explicitly directed by the human maintainer:

- Local-first Windows desktop application
- WPF desktop host
- WebView2 UI surface
- HTML/CSS/JS frontend assets
- Access-oriented backend
- Attachments and reports stored as files/folders, not embedded database binaries
- Outlook draft-only workflow
- End-user workflow must not require terminal usage on work machines
- Packaging must support a practical Windows workstation deployment path

Do **not** replace this with:

- cloud-first architecture
- local web server requirement
- mobile app architecture
- Electron rewrite
- random framework migration
- hosted database dependency
- SMTP/cloud email sending
- terminal-dependent user workflow

---

## 4. Non-negotiable agent rules

- Inspect before editing.
- Read `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md` first.
- Do not guess when files, scripts, or tests can be inspected.
- Do not rewrite the project from scratch.
- Do not remove features just to make checks pass.
- Do not invent business rules beyond the source of truth.
- Do not introduce unrelated frameworks or tooling.
- Do not claim features are implemented if they are scaffold-only.
- Do not claim build, package, runtime, workflow, or screenshot success unless verified.
- Do not hide failures or environment limits.
- Do not commit secrets, credentials, tokens, private machine data, or personal paths as real defaults.
- Do not break Windows runtime behavior while fixing Linux/cloud/static checks.
- Prefer minimal, durable, reviewable changes over broad rewrites.

---

## 5. Business rules that must not drift

Always preserve the rules in `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`, especially:

- User identity is read-only and comes from Windows/current runtime user.
- Settings is admin-only, not supervisor/operator.
- Diagnostics is admin-only, not supervisor/operator.
- Normal workflow must not expose Settings/Diagnostics.
- `Departments Completed` replaces old `Work Order Closure` wording.
- Total Efficiency / Total Yield / Total Downtime labels must remain exactly as written.
- Handover departments and Budget labour rows are different concepts.
- Budget View must include full operational staffing information, not just a simple variance table.
- Send workflow is Outlook draft-only and must never auto-send.

---

## 6. Repository boundaries

Use the repository structure intentionally:

- `desktop-host/` — Windows host runtime, WPF shell, WebView2 bootstrap, host bridge, filesystem/runtime integration.
- `webapp/` — HTML/CSS/JS UI routes, screen logic, client-side state, bridge calls.
- `backend/access/` — Access schema, seed data, setup/bootstrap artefacts, persistence model.
- `docs/` — operator guidance, developer guidance, runtime checklist, Windows manual testing notes.
- `scripts/` — repeatable helper scripts for prereq checks, web checks, build, package, and package verification.
- `.github/workflows/` — CI validation, especially Windows build/package checks.

Do not move responsibilities across these boundaries without a clear reason and matching documentation update.

---

## 7. Required task lifecycle

For every non-trivial task:

### A. Orient

Run or inspect equivalent:

```bash
git status
git branch --show-current
git log --oneline -n 10
```

Inspect the relevant files before editing.

### B. Plan

Create a short internal plan based on inspected files.

The plan must identify:

- scope
- affected files
- verification commands
- known environment limits

### C. Change

Make focused changes only. Prefer existing patterns in the repository.

### D. Verify

Run the strongest relevant checks available.

### E. Report

End with a clear summary of:

- what changed
- why it changed
- commands run
- pass/fail results
- screenshot evidence where relevant
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

1. **Verified** — command, screenshot, or runtime check was executed and passed.
2. **Partially verified** — static/build/package/browser checks passed, but real Windows runtime still needs manual validation.
3. **Not verified** — could not be tested in this environment, with exact reason.
4. **Blocked** — attempted but blocked by missing tool/runtime/permission, with exact blocker.

Never write vague statements such as:

- "should work"
- "looks good"
- "probably fixed"
- "verified" without evidence

Use evidence instead:

- command run
- result
- relevant output summary
- screenshot paths when applicable
- remaining limitation if any

---

## 10. Windows runtime constraints

The real runtime target is Windows with:

- WebView2 runtime
- Access Database Engine / ACE OLEDB behaviour where applicable
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

## 11. Screenshot evidence rules

When changing UI, regenerate screenshot evidence where relevant.

Screenshots must show realistic populated data where possible.

Do not use empty host-bridge-unavailable screens as main proof.

Clearly state whether screenshots are:

- real WPF/WebView2 runtime screenshots
- browser/mock screenshots
- static partial screenshots

Do not claim full Windows runtime verification from browser/mock screenshots.

---

## 12. Documentation rules

Documentation must match implementation reality.

Update docs when:

- commands change
- scripts change
- package output changes
- runtime requirements change
- manual test steps change
- a feature is implemented, deferred, or removed
- source-of-truth behaviour changes

Do not document scaffold-only features as complete.

---

## 13. Security and safety rules

Always check for:

- committed secrets
- hardcoded credentials
- unsafe shell execution
- path traversal risk
- arbitrary file overwrite risk
- unsafe attachment handling
- accidental real email sending
- raw user/persisted text inserted into `innerHTML` without escaping
- logs exposing sensitive local information

Use safe defaults and clear validation.

---

## 14. Final report format

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

## Screenshot evidence
- screenshot path: what it proves, and whether browser/mock or WPF/WebView2

## Windows runtime status
- Verified:
- Partially verified:
- Not verified:
- Blocked:

## Remaining risks
- Real remaining risks only

## Next action
- One clear recommended next step
```

Do not bury failures. Put blockers and unverified runtime areas in the final report.

---

## 15. Definition of done

A task is done only when:

- `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md` was inspected
- implementation changes are complete for the requested scope
- relevant checks were run or honestly marked blocked
- docs were updated if behavior changed
- no known critical/high issue introduced by the change remains unresolved
- Windows-only limitations are clearly identified
- final report includes evidence, not vague confidence
