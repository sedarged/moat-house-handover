# AGENTS.md — MOAT HOUSE HANDOVER v2

This file defines the root working rules for Codex Web, Codex CLI, Claude Code, and other coding agents in this repository.

The goal is simple: protect the source of truth, avoid fake progress, make real reviewable changes, and report verification honestly.

---

## 1. Required read order

Before planning or editing, read these files in this order:

1. `AGENTS.md`
2. `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`
3. `docs/agents/CODEX_WEB_GUIDE.md` when using Codex Web
4. `docs/AI_TASK_QUEUE.md`
5. phase-specific docs such as `docs/ACCESS_TO_SQLITE_MIGRATION_PLAN.md`
6. nearest folder-level `AGENTS.md`, if one exists

For deep UI/review/correction work, also read:

- `MASTER_TASK_DEEP_REVIEW_UI_UX.md`

Conflict order:

1. `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md` wins for product/business/workflow/UI rules.
2. `docs/decisions/*` ADRs win for approved architecture decisions.
3. nearest folder-level `AGENTS.md` wins for folder-specific rules.
4. current human maintainer instruction wins when it explicitly changes scope.

The old fragmented plan/spec files are retired. Do not use deleted `01_*` to `07_*` documents as source of truth.

---

## 2. Current locked architecture summary

The current approved direction is:

- local-first Windows desktop application
- WPF desktop host
- WebView2 UI surface
- HTML/CSS/JS frontend assets
- SQLite local database target
- Access legacy/current runtime until phased migration completion
- primary live data root: `M:\Moat House\MoatHouse Handover\`
- attachments and reports stored as files/folders
- Outlook draft-only workflow
- end-user workflow must not require terminal usage on work machines
- practical Windows workstation deployment path

Do not introduce architecture drift such as:

- SQL Server for now
- hosted backend server
- cloud database
- SignalR/server dependency
- local web server requirement
- SMTP/cloud sending
- Electron rewrite
- browser-only deployment
- terminal-dependent operator workflow

Access-to-SQLite migration must follow:

- `docs/decisions/ADR-001-local-sqlite-database.md`
- `docs/ACCESS_TO_SQLITE_MIGRATION_PLAN.md`

Do not replace Access by hidden refactor.

---

## 3. Codex Web execution contract

For implementation work in Codex Web, use **CODE MODE**.

Advice-only responses are acceptable only when the task explicitly says Ask/analysis only.

For implementation tasks, Codex must either:

1. modify the repository and create/update a PR, or
2. stop and report `NO CODE CHANGES MADE — BLOCKED` with the exact blocker.

Do not claim a change was made unless it is visible in at least one of:

- changed file diff
- commit
- updated PR metadata verified after update
- generated artifact path
- command output

Words are not evidence.

If a phase requires implementation across multiple files, do not satisfy it with metadata-only or one-line edits unless the phase explicitly says docs-only.

If the phase cannot be completed safely, stop before committing and report the blocker plus the smaller phase that should be done next.

---

## 4. Phase and PR discipline

Default working model:

- one task = one phase
- one phase = one PR
- one PR = one clear scope

Before editing, identify:

- active branch
- source-of-truth files read
- relevant files inspected
- intended files to change
- exact acceptance criteria
- exact verification commands

During editing:

- stay inside the requested phase
- follow existing repository patterns
- do not remove features to make checks pass
- do not invent business rules
- do not leave placeholder-only work unless scaffold-only was requested
- do not change UI/design during storage/database/deployment work unless explicitly requested

After editing:

1. check changed files
2. run required verification or mark it blocked with exact reason
3. update docs if behaviour changed
4. make PR metadata match the actual diff
5. report Windows-only validation limits honestly

---

## 5. Verification commands

Prefer existing helper scripts over ad hoc commands.

For repo checks, use the verification matrix:

- `docs/agents/VERIFICATION_MATRIX.md`

Common commands:

```bash
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If `.NET` is missing and the repo helper is available:

```bash
bash scripts/bootstrap-dotnet.sh
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

For Windows CI reference, see:

- `.github/workflows/windows-build.yml`

Do not invent commands that do not exist.

---

## 6. Verification honesty

Final reports must separate:

1. **Verified** — command, screenshot, or runtime check was executed and passed.
2. **Partially verified** — static/build/package checks passed, but real Windows runtime still needs manual validation.
3. **Not verified** — could not be tested in this environment, with exact reason.
4. **Blocked** — attempted but blocked by missing tool/runtime/permission, with exact blocker.

Do not claim full Windows runtime verification unless tested on a real Windows workstation.

Usually real Windows validation is required for:

- interactive WPF/WebView2 launch
- Access/ACE/OLEDB provider behaviour
- Outlook COM draft creation
- Windows Explorer folder opening
- shared folder/network permissions
- packaged app execution by real users

---

## 7. Security and safety checks

Always check relevant changes for:

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

## 8. Required final status

End every implementation task with exactly one of:

- `PHASE READY FOR REVIEW`
- `PHASE NOT READY — BLOCKERS REMAIN`
- `NO CODE CHANGES MADE — BLOCKED`
- `NO CODE CHANGES MADE — ANALYSIS ONLY`

Do not use `READY TO MERGE` unless the user explicitly asked for merge readiness and all required checks/evidence pass.

---

## 9. Final report format

Use this structure for substantial implementation tasks:

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
- Blocked:

## Evidence
- Commit SHA:
- Changed files:
- PR metadata confirmed after update: yes/no/not applicable

## Remaining risks
- Real remaining risks only

## Next action
- One clear recommended next step
```

Do not bury failures. Put blockers and unverified runtime areas in the final report.
