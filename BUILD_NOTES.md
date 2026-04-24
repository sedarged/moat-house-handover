# Build Notes (Stage 1 Foundation)

This repository currently contains **Stage 1 foundation only** for MOAT HOUSE HANDOVER v2.

## What exists now (implemented)
- Windows desktop host skeleton (C# + WebView2 design)
- Static HTML/CSS/JS app shell with placeholder screens
- State and service interfaces/stubs aligned to spec
- Access backend schema/setup artifacts (design-first)
- Config templates for database and file roots

## Stage boundary (explicit)
### Intentionally stubbed in Stage 1
- Service operations (`openSession`, `saveDepartment`, attachment/report/send methods) are stubs and not wired to persisted runtime data.
- UI screens are placeholders with route/layout contracts, not real production workflows.

### Deferred to Stage 2 / Stage 3
- Stage 2: executable Access setup/bootstrap and approved business lookup seeding.
- Stage 3: DAO/query/repository wiring and real service implementations.
- No production DAO wiring exists yet.
- No real attachment workflow exists yet.
- No real report generation workflow exists yet.
- No real send/draft workflow exists yet.

## WebView2 index path note (Stage 1 only)
Current host loading of `webapp/index.html` uses a relative developer scaffold path.
This is acceptable for Stage 1 bootstrap/dev only.
A stable deployment/runtime asset path strategy for real work-machine installs is deferred to a later stage.
