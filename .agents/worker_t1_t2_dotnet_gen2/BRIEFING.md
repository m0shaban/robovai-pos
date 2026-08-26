# BRIEFING — 2026-08-08T06:30:00Z

## Mission
Write automated xUnit unit and integration tests covering C# backend requirements (Features 1-3, 9-12, 14-16 across Tiers 1 and 2) in `src/SmartPOS.UnitTests/`.

## 🔒 My Identity
- Archetype: test-writer
- Roles: specialist, qa
- Working directory: f:\Raw\kasher\kasher\.agents\worker_t1_t2_dotnet_gen2
- Original parent: 6ea71bb2-7558-4f52-aec0-c01ad40dbab2
- Milestone: M0

## 🔒 Key Constraints
- Write test code ONLY — never implementation code unless fixing test defects.
- DO NOT CHEAT. No facade tests, no hardcoded passing assertions without real verification.
- Test files in `src/SmartPOS.UnitTests/`.
- Ensure `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj` passes cleanly.

## Current Parent
- Conversation ID: 6ea71bb2-7558-4f52-aec0-c01ad40dbab2
- Updated: 2026-08-08T06:30:00Z

## Task Summary
- **What to build**: Comprehensive unit and integration test suite covering C# backend requirements for Tiers 1 & 2 (Features 1-3, 9-12, 14-16).
- **Success criteria**: All tests compile and pass via `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj`.
- **Interface contracts**: Defined in PROJECT.md, SCOPE.md, TEST_INFRA.md, analysis docs.

## Key Decisions Made
- Will inspect existing codebase and test files first using code-review-graph and file readers.

## Artifact Index
- DISPATCH.md — Prompt instructions
- BRIEFING.md — Context memory
- progress.md — Heartbeat progress
- handoff.md — Final handoff report
