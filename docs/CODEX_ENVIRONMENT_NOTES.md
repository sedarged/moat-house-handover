# CODEX Environment Notes

## Expected tools for this repository
The standard toolchain expected for normal development and validation is:
- `dotnet`
- `node`
- `git`

If `dotnet` is not installed system-wide, use `scripts/bootstrap-dotnet.sh` to install a local SDK into `./.dotnet` for repository build/package scripts.

## Cloud vs real runtime differences
Codex cloud environments are useful for editing, static checks, and partial build validation, but they are not identical to workstation runtime.

Important gap areas:
- Windows-specific host runtime behavior
- WebView2 runtime integration behavior
- Access Database Engine (ACE/OLEDB/COM) runtime behavior

These still require verification on a real Windows machine.

## Honesty requirements for task execution
- If a required tool is missing, report it explicitly.
- Do not hide missing-tool limitations behind generic success statements.
- Do not claim Windows runtime verification unless it was actually performed on Windows.

## Execution expectation in constrained environments
When possible, complete code/documentation/script changes even if full runtime checks are blocked by environment limitations.
Then clearly separate what was executed vs what still requires Windows validation.
