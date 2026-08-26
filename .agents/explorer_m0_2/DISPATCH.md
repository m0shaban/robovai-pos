## 2026-08-08T06:05:59Z
<USER_REQUEST>
You are an Explorer mapping API endpoints and data model contracts for E2E testing for M0 (E2E Testing Track).
Working directory: f:\Raw\kasher\kasher\.agents\explorer_m0_2
Project root: f:\Raw\kasher\kasher

MUST READ:
- Original request: f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md
- Project doc: f:\Raw\kasher\kasher\PROJECT.md
- Scope doc: f:\Raw\kasher\kasher\.agents\sub_orch_m0\SCOPE.md

Task:
1. Map out all interface contracts, HTTP endpoints (`/api/v1/pair/handshake`, `/api/v1/sync/export-stream`, `/api/v1/sync/import-stream`, `/api/v1/devices/heartbeat`), database schemas (`sync_outbox`, `Branch`, `ConnectedDevice`, Dexie IndexedDB schemas), and UI components (Bento grid, Compose theme, bottom nav, QR token, Ad dialog).
2. Determine how each of the 18 features listed in PROJECT.md can be programmatically tested and verified (HTTP requests, mock servers, script assertions, CLI test harnesses).
3. Write your detailed findings to f:\Raw\kasher\kasher\.agents\explorer_m0_2\analysis.md and your handoff report to f:\Raw\kasher\kasher\.agents\explorer_m0_2\handoff.md.

Send a message back to parent when done.
</USER_REQUEST>
