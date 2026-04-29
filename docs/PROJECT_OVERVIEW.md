# Project Overview

## Approved target architecture
- Windows desktop app
- WPF / .NET desktop host
- WebView2 frontend
- SQLite local database target
- local-first deployment

## Current implementation state
- Access is legacy/current implementation until migration completes.
- Migration must follow ADR-001 and phased PR sequence.

## Explicit non-goals
- No SQL Server
- No hosted backend server
- No cloud database
- No SignalR dependency
- No Electron rewrite
- No browser-only deployment

## Design protection
Do not redesign the UI. Preserve the current modern Moat House dark/orange WebView2 design. Database/storage/deployment work must not change screen layout, colours, spacing, cards, buttons, badges, hover states, or workflow visuals unless explicitly requested.

## Storage protection
Primary live data root is `M:\Moat House\MoatHouse Handover\`.
