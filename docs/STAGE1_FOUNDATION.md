# Stage 1 Foundation Notes

This stage intentionally establishes architecture and integration seams without completing business features.

## Architecture locked in
- Local-first desktop shell with WebView2 host
- HTML/CSS/JS front-end
- Access-oriented backend design
- Attachments/reports stored in file system roots

## Explicit stage boundary
### Implemented now
- Desktop host scaffold + web shell scaffold
- Placeholder routes/screens for all major flows
- Model contracts and service interfaces/stubs
- Access schema/seed/setup design artifacts

### Intentionally stubbed
- DAO-backed service behavior
- Real business save/load logic
- Real end-to-end attachment/report/send logic

### Deferred to Stage 2 / Stage 3
- Stage 2: executable Access bootstrap + approved lookup data
- Stage 3: DAO repositories + service runtime wiring

No production DAO wiring exists in Stage 1.
No real attachment/report/send workflow exists in Stage 1.
