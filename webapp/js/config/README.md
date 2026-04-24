# Configuration

`app.config.template.json` defines required runtime path keys used by the desktop host.

## Required keys
- `accessDatabasePath`
- `attachmentsRoot`
- `reportsOutputRoot`

## Stage 2A behavior
- Desktop host loads runtime JSON from local file paths.
- Required keys are validated during host startup.
- Startup halts with an error if required config is missing/invalid.
- The web app receives runtime status through the host bridge (`runtime.getStatus`).
