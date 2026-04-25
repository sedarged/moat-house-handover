# AGENTS.md — MOAT HOUSE HANDOVER v2

## Project goal
Maintain and extend MOAT HOUSE HANDOVER v2 as a **local-first Windows desktop handover application** while preserving the existing business workflow and stage plan from the spec set.

## Read-first rule (single source of truth)
Before planning or editing code, read these files first and treat them as authoritative:
- `01_MASTER_SPEC.md`
- `02_SCREEN_BLUEPRINT.md`
- `03_DATA_MODEL.md`
- `04_SERVICE_MAP.md`
- `05_BUILD_ORDER.md`
- `06_ACCEPTANCE_TESTS.md`
- `07_CLAUDE_CODE_PROMPTS.md`

Then read current implementation context:
- `README.md`
- `BUILD_NOTES.md`
- `docs/*`
- `desktop-host/*`
- `webapp/*`
- `backend/access/*`

## Locked architecture (must not drift)
Keep this architecture unchanged unless explicitly directed by human maintainers:
- Local-first Windows desktop application
- WPF desktop host with WebView2
- HTML/CSS/JS UI
- Access-oriented backend (DAO/Access SQL path)
- Attachments and reports stored as files/folders, not embedded DB binaries
- End-user workflow must not require terminal usage on work machines

## Non-negotiable rules
- Do not replace the architecture with web server/cloud-first/mobile stacks.
- Do not invent or implement future business stages early.
- Do not introduce random frameworks or tooling unrelated to current scope.
- Do not claim features are implemented if they are scaffold-only.
- Do not claim runtime verification that was not actually performed.

## Stage discipline
- Follow `05_BUILD_ORDER.md` stage boundaries.
- Implement only the requested stage/scope.
- Explicitly label deferred work rather than silently extending scope.

## Runtime constraints
- Real runtime target is Windows with WebView2 and Access Database Engine behavior.
- Cloud/Linux environments are for code editing and partial checks only.
- WebView2 and ACE/OLEDB behavior still require real Windows verification.

## Repository boundaries
- `desktop-host/`: Windows host runtime/bootstrap and host bridge.
- `webapp/`: HTML/CSS/JS UI routes, screen logic, client services.
- `backend/access/`: schema/seed/setup artifacts for Access backend.
- `docs/`: stage notes, continuation notes, operator/developer guidance.
- `scripts/`: reusable helper scripts for repeatable checks/build/package flows.

## Environment honesty rules
- Inspect available tools first (`dotnet`, `node`, `git`) before claiming verification.
- If a required tool is missing, state it clearly and continue with achievable work.
- When possible, use repo bootstrap helpers (for example `scripts/bootstrap-dotnet.sh`) before declaring a hard block.
- Never report build/runtime success when commands were not run.
- Separate: (1) code changed, (2) commands run, (3) commands blocked by environment.

## Working style for agents
- Prefer helper scripts in `scripts/` over ad hoc one-off commands.
- Keep changes minimal, durable, and stage-aligned.
- Update docs when workflow/tooling behavior changes.
- Preserve local-first deployment assumptions.
