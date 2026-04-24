# Desktop Host (WebView2 Foundation)

This folder holds the Windows desktop host skeleton that will load the `webapp` assets into WebView2.

## Stage 1 status
- Project file and entry point scaffolded
- Config loading contract defined
- Browser host wiring shown as implementation target
- No production IPC bridge implementation yet

## Index path strategy note
The current `MainWindow` implementation loads `webapp/index.html` from a relative scaffold path to support Stage 1 development.
This is **not** the final deployment/runtime asset resolution strategy.
A production-safe path strategy for packaged local workstations will be implemented in a later stage.
