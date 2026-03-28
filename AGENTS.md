# Recall-mcp Agent Notes

## Scope
- These instructions apply to the entire repository.

## Project Shape
- `src/MemoryMcp.Server` hosts the MCP stdio server and tool registrations.
- `src/MemoryMcp.Application` contains service-layer logic and DTOs.
- `src/MemoryMcp.Infrastructure` contains SQLite persistence, vector search, import/export, and AI provider integrations.
- `tests/*` mirrors the main layers; add regression tests near the changed behavior.

## Working Rules
- Keep changes narrow and rooted in the actual failure mode.
- Prefer fixing service/repository/provider behavior over adding tool-layer workarounds.
- Preserve current naming and file layout unless a real refactor is required.
- Do not emit anything to stdout during MCP server startup except protocol traffic.

## Validation
- For C# validation, prefer the `csharp-analysis` MCP tools first.
- Run targeted tests before broader suites.
- If persistence or MCP behavior changes, verify with both tests and a live stdio MCP smoke path when practical.

## Logging and Runtime
- Logs and database live under `app-data/` in the main configured setup.
- Keep logs useful for diagnosis; prefer structured messages with ids, namespaces, provider names, and outcomes.
- Warning-level fallback logs are acceptable when external AI/network calls fail and the app continues locally.

## AI Provider Behavior
- This project must remain usable without network access.
- External embedding/summarization providers should degrade to local deterministic/simple behavior instead of crashing.
- Keep GapGPT/OpenAI-compatible configuration behavior aligned with the current README and Codex MCP config.
