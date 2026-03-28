# MemoryMcp

Local-first MCP persistent memory server built on .NET 10.

## Features

- CRUD memory operations with version history
- Namespace, tag, type, and date filtering
- Semantic search with deterministic local embeddings and Ollama fallback
- Full-text search, stale-memory reporting, and expired-memory cleanup
- Directional memory links
- Conversation summarization with optional persistence
- JSON and Markdown export plus JSON import
- Serilog console and rolling file logging

## Run

```bash
dotnet run --project src/MemoryMcp.Server
```

The server uses stdio transport and reads configuration from `src/MemoryMcp.Server/appsettings.json`.

### GapGPT / OpenAI-compatible API

To use a custom OpenAI-compatible endpoint such as `https://api.gapgpt.app/v1`:

```json
"Embedding": {
  "Provider": "GapGpt",
  "Model": "text-embedding-3-small",
  "BaseUrl": "https://api.gapgpt.app/v1",
  "ApiKey": ""
},
"AI": {
  "Provider": "GapGpt",
  "Model": "gapgpt-qwen-3.5",
  "BaseUrl": "https://api.gapgpt.app/v1",
  "ApiKey": ""
}
```

Prefer environment variables for secrets:

```bash
export MEMORYMCP_AI_API_KEY="your-gapgpt-key"
export MEMORYMCP_EMBEDDING_API_KEY="your-gapgpt-key"
```

`AI` drives conversation summarization. `Embedding` drives semantic search, deduplication, and contradiction detection.

## Main MCP tools

- `store_memory`
- `get_memory`
- `update_memory`
- `delete_memory`
- `list_memories`
- `search_memories`
- `link_memories`
- `wipe_namespace`
- `find_stale_memories`
- `cleanup_expired_memories`
- `summarize_conversation`
- `export_memories`
- `import_memories`

## Validation

```bash
dotnet build MemoryMcp.slnx
dotnet test MemoryMcp.slnx
```
