# WINDOWS_CI_NOTES

## Purpose of GitHub-hosted Windows CI

The Windows GitHub Actions workflow (`.github/workflows/windows-build.yml`) provides a repeatable baseline check on a real Windows runner for:
- Web syntax validation
- Desktop host build with Windows targeting
- Local packaging output generation
- Packaged asset presence checks
- Artifact publishing for review/download

This gives early detection of packaging/build regressions before manual workstation validation.

## CI build validation vs. workstation runtime validation

GitHub-hosted CI confirms that repository code can compile/package in a clean Windows environment.
It does **not** guarantee full behavior in production-like workstation conditions.

### CI build validation covers
- Build/publish success in ephemeral Windows runner environment
- Presence of required packaged static/config assets

### Workstation runtime validation still required for
- Outlook COM draft creation behavior with real Outlook desktop configuration
- ACE/OLEDB runtime behavior as installed on user machines
- Interactive WebView2 behavior under real operator usage
- Shared drive and permissions behavior (network paths, ACLs, org policy constraints)

## When to use a self-hosted Windows runner

Use a self-hosted Windows runner when validation needs installed enterprise dependencies or environment parity that GitHub-hosted runners do not provide by default.

Typical triggers:
- Need to validate with a specific Office/Outlook build and profile policy
- Need to validate with specific Access Database Engine installation/version
- Need network path/access checks against corporate shares
- Need organization-specific desktop hardening policy compatibility checks

Even with CI green, final release confidence still depends on executing `docs/LOCAL_WINDOWS_RUNBOOK.md` and `docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md` on representative workstations.
