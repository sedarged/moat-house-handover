# Desktop Host (Stage 2A Runtime)

This folder holds the Windows WPF + WebView2 host that loads local web assets and initializes local runtime dependencies.

## Stage 2A status
- Runtime config JSON loading with validated required keys
- Config path lookup strategy suitable for packaged workstation deployment
- Startup initializer service for folder checks and Access bootstrap
- Idempotent Access schema + approved seed setup path
- Bootstrap logging
- Host-to-web message bridge skeleton

## Runtime asset strategy
- Primary: `<app>/webapp/index.html` from packaged output
- Fallback: repository `webapp/index.html` for local development
- No local web server required

## Runtime config file
Packaged default:
- `<app>/config/runtime.config.json`

Development default:
- `desktop-host/config/runtime.config.json`
