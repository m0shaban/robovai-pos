## 2026-08-08T09:05:23Z
You are an Explorer agent for Milestone M1 (Outbox Queue & Sync Engine).
Your working directory is: f:\Raw\kasher\kasher\.agents\explorer_m1_2

MUST READ:
- f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md
- f:\Raw\kasher\kasher\PROJECT.md
- f:\Raw\kasher\kasher\.agents\explorer_3\analysis.md

Task:
Investigate existing code and plan the exact implementation for:
1. `sync_outbox` table in SQLite / EF Core (and Dexie.js if relevant for web components).
2. Outbox entity, DTOs, repository/data access layer, change tracking mechanism.
3. Outbox background sync processor/engine for handling outbox entries and syncing with server / local queue.

Provide concrete file paths, schema definitions, C# model & service code structures, and implementation details.
Write your report to `f:\Raw\kasher\kasher\.agents\explorer_m1_2\analysis.md` and write your handoff report to `f:\Raw\kasher\kasher\.agents\explorer_m1_2\handoff.md`.
Send a message to the caller when done with the path to your handoff file.
