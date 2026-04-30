# Codex Environment Guide

This document describes the expected Codex Web / cloud task environment for this repository.

It is not a replacement for real Windows workstation validation.

## 1. Environment purpose

Codex Web is useful for:

- repository inspection
- code edits
- docs updates
- static frontend checks
- Windows-targeted .NET build/package checks where available
- PR preparation
- CI-driven validation

Codex Web is not sufficient by itself to fully verify:

- interactive WPF/WebView2 runtime launch
- Access Database Engine / ACE OLEDB installed runtime behaviour
- Outlook COM draft creation
- mapped/shared `M:\` drive availability and permissions
- packaged app execution by real users on a workstation

## 2. Expected tools

Useful Codex Web tasks should have access to:

- Git
- Bash
- Node.js 20 or compatible
- .NET 8 SDK, or ability to run `bash scripts/bootstrap-dotnet.sh`
- PowerShell when the environment supports it

Prefer repository helper scripts over ad hoc commands.

## 3. Repository commands

From repo root:

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

Do not invent commands. Inspect scripts before changing or using them in a new way.

## 4. Dependency and network notes

Use checked-in code and scripts first.

If a Codex environment needs internet during setup, keep it conservative and task-specific. Typical legitimate dependency/update domains may include:

- `github.com`
- `api.github.com`
- `raw.githubusercontent.com`
- `dotnet.microsoft.com`
- `builds.dotnet.microsoft.com`
- `download.visualstudio.microsoft.com`
- `packages.microsoft.com`
- `api.nuget.org`
- `www.nuget.org`
- `nodejs.org`
- `registry.npmjs.org`

Do not fetch arbitrary third-party scripts and execute them blindly.

Do not commit downloaded dependency caches or build artifacts unless explicitly required.

## 5. Sensitive information and local paths

Never commit credentials, private machine data, private workstation folders, or personal environment details as production defaults.

The approved production live data root is:

```text
M:\Moat House\MoatHouse Handover\
```

Do not silently fall back to:

```text
C:\ProgramData
%TEMP%
%LOCALAPPDATA%
```

unless a future explicit admin override has been approved and documented.

## 6. Windows runtime reporting

When Codex runs in cloud/Linux/non-workstation context, final reports must say:

```text
Windows runtime status:
Verified: none
Partially verified: static/build/package checks only
Not verified: WPF/WebView2 interactive launch, ACE/OLEDB runtime, Outlook COM, M:\ shared-drive permissions
Blocked: none / exact blocker
```

Only claim a Windows runtime item is verified if it was actually tested on a real Windows workstation.

## 7. CI relationship

GitHub Actions workflow:

```text
.github/workflows/windows-build.yml
```

is the CI source for Windows build/package validation.

It does not replace real manual workstation validation for Outlook, WebView2 interactivity, ACE/OLEDB installed runtime behaviour, or mapped drive permissions.

## 8. Environment blockers

If setup/build/test cannot run, report exactly:

- command attempted
- exact error
- whether bootstrap was attempted
- whether the blocker prevents implementation or only limits verification
- next action needed

Use final status:

```text
PHASE NOT READY — BLOCKERS REMAIN
```

or:

```text
NO CODE CHANGES MADE — BLOCKED
```

when appropriate.
