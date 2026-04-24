# Build Notes (Stage 1 Foundation)

This repository currently contains **Stage 1 foundation only** for MOAT HOUSE HANDOVER v2.

## What exists
- Windows desktop host skeleton (C# + WebView2 design)
- Static HTML/CSS/JS app shell with placeholder screens
- State and service interfaces/stubs aligned to spec
- Access backend schema/setup artifacts (design-first)
- Config templates for database and file roots

## Local run intent (next stage)
1. Build the desktop host (`desktop-host`) on Windows with .NET + WebView2 Runtime.
2. Host loads `webapp/index.html` into WebView2.
3. UI calls service interfaces; service implementations will be connected to Access in Stage 2/3.

## Stage boundary
No production data access, report generation, or Outlook send integration is implemented in Stage 1.
