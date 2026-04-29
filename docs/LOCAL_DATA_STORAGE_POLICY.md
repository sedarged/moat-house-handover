# Local Data Storage Policy

## Primary live data root
The primary live operational data root for MOAT HOUSE HANDOVER v2 is:

`M:\Moat House\MoatHouse Handover\`

Required folder structure:

- `M:\Moat House\MoatHouse Handover\Data\`
- `M:\Moat House\MoatHouse Handover\Attachments\`
- `M:\Moat House\MoatHouse Handover\Reports\`
- `M:\Moat House\MoatHouse Handover\Backups\`
- `M:\Moat House\MoatHouse Handover\Logs\`
- `M:\Moat House\MoatHouse Handover\Config\`
- `M:\Moat House\MoatHouse Handover\Imports\`
- `M:\Moat House\MoatHouse Handover\Migration\`

Target SQLite database path:

- `M:\Moat House\MoatHouse Handover\Data\moat-house.db`

## Rules
- Do not use `C:\ProgramData` as the primary live data root.
- `C:\ProgramData` may only be treated as a future fallback/admin override path, not the default production location.
- No server-hosted data path is introduced by this policy.

## Shared drive caution
If `M:` is a shared/network-backed path, do not assume SQLite WAL mode as the default. Start with conservative SQLite settings and only enable WAL when runtime checks confirm the DB path is local non-network storage.
