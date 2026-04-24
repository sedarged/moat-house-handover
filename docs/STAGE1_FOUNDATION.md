# Stage 1 Foundation Notes

This stage intentionally establishes architecture and integration seams without completing business features.

## Architecture locked in
- Local-first desktop shell with WebView2 host
- HTML/CSS/JS front-end
- Access-oriented backend design
- Attachments/reports stored in file system roots

## Next-stage safety notes
- Keep screen IDs/routes stable to avoid breaking host navigation and test scripts.
- Keep model contracts in `webapp/js/models/contracts.js` as canonical UI payload shape.
- Implement service adapters by filling in `webapp/js/services/*Service.js` files, not by bypassing them in screens.
- Convert schema artifacts to executable Access setup script/automation before enabling data writes.
