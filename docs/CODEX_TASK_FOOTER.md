# Reusable Codex Task Footer

## Environment honesty and tool inspection
1. Inspect available tools first (`dotnet`, `node`, `git`) and report what is available.
2. If a required tool is missing, state that clearly before reporting verification results.
3. Do not claim build/runtime success for commands that were not executed.

## Required completion reporting format
Always separate your final report into these sections:

1. **Code changes completed**
2. **Commands actually run**
3. **Commands not run (environment limitations)**
4. **Must verify later on real Windows machine**

## Windows verification reminder
Explicitly call out any runtime behavior that remains unverified without real Windows execution, including:
- WebView2 behavior
- ACE/OLEDB/COM Access runtime behavior
- final packaged desktop run-through
