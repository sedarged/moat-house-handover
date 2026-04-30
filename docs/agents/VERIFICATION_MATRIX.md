# Verification Matrix

Use the strongest relevant checks available for the task type.

Do not claim verification that did not actually happen.

## Status vocabulary

| Status | Meaning |
|---|---|
| Verified | Command, screenshot, or runtime check was executed and passed. |
| Partially verified | Static/build/package checks passed, but real Windows runtime still needs manual validation. |
| Not verified | Could not be tested in this environment; explain why. |
| Blocked | Attempted but blocked by a specific missing tool, runtime, permission, or decision. |

## Task type matrix

| Task type | Required checks | Notes |
|---|---|---|
| Docs-only PR | Inspect changed markdown/yaml. Confirm no runtime code changed. | Do not run or claim runtime checks unless needed. |
| Agent instruction/template PR | Inspect `AGENTS.md`, `docs/agents/*`, `.github/*` templates. Confirm instructions do not conflict with source of truth. | Keep instructions concise and aligned with ADRs/source-of-truth. |
| Frontend JavaScript/static UI | `bash scripts/check-web.sh` | If visual UI changed, provide screenshot evidence. Browser/mock screenshots are not WPF/WebView2 runtime proof. |
| C# desktop host | `bash scripts/check-prereqs.sh`, `bash scripts/build-host.sh`, `bash scripts/package-local.sh`, `bash scripts/verify-package-assets.sh` | If `.NET` missing, try `bash scripts/bootstrap-dotnet.sh` first. |
| Storage/path/config service | Same as C# desktop host checks. | Real `M:\` shared-drive permissions require Windows/workstation validation. |
| Access/ACE runtime changes | C# host checks plus real Windows workstation validation where possible. | Cloud/Linux checks cannot prove ACE/OLEDB provider availability. |
| SQLite implementation | C# host checks plus any DB tests/importer validation added by the phase. | Do not switch runtime default until approved phase. Avoid WAL assumption for `M:\` shared/network path. |
| Packaging/deployment | `bash scripts/package-local.sh`, `bash scripts/verify-package-assets.sh`, Windows workflow must pass. | Installer/updater behaviour still needs real workstation testing. |
| Outlook draft workflow | C# host checks plus real Windows + Outlook desktop validation. | Never claim Outlook COM draft creation from cloud-only checks. |
| WebView2 interactive runtime | Package checks plus real Windows WPF/WebView2 launch. | Browser screenshot is not full runtime proof. |
| Screenshot evidence PR | Generate/update required screenshots, list exact paths. | State whether screenshots are browser/mock or real WPF/WebView2. |

## Common command set

Run from repository root when relevant:

```bash
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If `.NET` is missing:

```bash
bash scripts/bootstrap-dotnet.sh
bash scripts/check-prereqs.sh
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

## Windows-only validation boundaries

The following are not fully verified by cloud/Linux-only execution:

- WPF window launch
- WebView2 interactive UI behaviour
- Access Database Engine / ACE OLEDB provider behaviour
- Outlook COM draft creation
- Windows Explorer folder opening
- `M:\` mapped/shared drive availability and permissions
- packaged app execution by real users

When these are not tested, write:

```text
Windows runtime status:
Verified: none
Partially verified: build/package/static checks only
Not verified: WPF/WebView2 interactive launch, ACE/OLEDB runtime, Outlook COM, M:\ shared-drive permissions
Blocked: none / exact blocker
```

## Blocked command reporting

If a command cannot run, include:

- command attempted
- exact error/blocker
- whether this blocks the phase or only limits verification
- recommended next action

Example:

```text
bash scripts/build-host.sh: BLOCKED — dotnet command not found. Attempted bootstrap-dotnet.sh but download failed due network policy. This blocks C# verification.
```

## Screenshot evidence requirements

When UI changed, final report must include:

- screenshot path
- what it proves
- whether it is browser/mock or real WPF/WebView2
- any missing required screenshot and why

Do not use empty host-bridge-unavailable screens as primary proof of functional UI.
