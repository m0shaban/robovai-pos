## 2026-08-08T06:29:43Z
You are a replacement Test Writer Worker (Gen 2) implementing .NET Backend xUnit tests for M0 E2E Testing Track.
Working directory: f:\Raw\kasher\kasher\.agents\worker_t1_t2_dotnet_gen2
Project root: f:\Raw\kasher\kasher

MUST READ BEFORE STARTING:
- Original request: f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md
- Project doc: f:\Raw\kasher\kasher\PROJECT.md
- Scope doc: f:\Raw\kasher\kasher\.agents\sub_orch_m0\SCOPE.md
- Test Infra doc: f:\Raw\kasher\kasher\TEST_INFRA.md
- Spec Miner analysis: f:\Raw\kasher\kasher\.agents\spec_miner_m0_1\analysis.md
- Contract analysis: f:\Raw\kasher\kasher\.agents\explorer_m0_2\analysis.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Task:
1. In `src/SmartPOS.UnitTests/`, write automated xUnit unit and integration test files covering C# backend requirements (Features 1-3, 9-12, 14-16 across Tiers 1 and 2):
   - Multi-Mode Sync Config Engine & `sync_outbox` transactional queue logic
   - Embedded Kestrel HTTP server listeners & endpoints (`/api/v1/pair/handshake`, `/api/v1/sync/*-stream`, `/api/v1/devices/heartbeat`)
   - `IDbContextFactory` scoping & change tracker cleanups
   - SQLite WAL mode (`PRAGMA journal_mode=WAL;`) and busy timeout (`busy_timeout=30000;`) lock checks
   - GC compaction & live chart paint recycling / camera handle cleanup
   - Branch, ConnectedDevice, and BranchStock EF Core domain schemas and heartbeats
2. Execute `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj` from project root. Fix any failing tests or compiler errors until all tests pass.
3. Write your completion report and test execution results to f:\Raw\kasher\kasher\.agents\worker_t1_t2_dotnet_gen2\handoff.md.

Send a message back to parent when done.
