# MemoryMcp

`MemoryMcp` is a local-first MCP server for persistent memory. It lets an MCP client store facts, preferences, instructions, context, and conversation summaries, then retrieve them later with both exact filtering and semantic search.

The project is built on .NET 10 and follows a pragmatic clean-architecture split:

- `src/MemoryMcp.Domain` contains entities and value objects
- `src/MemoryMcp.Application` contains use-case services and contracts
- `src/MemoryMcp.Infrastructure` contains SQLite, vector search, export/import, and AI integrations
- `src/MemoryMcp.Server` hosts the MCP tools over stdio

## Why This Project Exists

LLM sessions are transient. Useful facts often need to survive beyond a single conversation. This server gives an MCP client a structured memory layer that can:

- persist durable information in SQLite
- attach metadata such as namespace, type, tags, and expiration
- support semantic lookup instead of only exact keyword match
- summarize raw conversation text into compact notes
- detect duplicates and possible contradictions before or after storing

## How It Works

At a high level, an MCP client calls a tool such as `store_memory` or `search_memories`. The server translates that tool call into an application service call, persists data in SQLite, and optionally uses AI services for embeddings or summarization.

### Store Flow

When a client stores memory:

1. The server receives `store_memory`
2. `MemoryService` creates a `MemoryEntry`
3. The configured embedding provider converts the content into a vector
4. The entry is saved in SQLite with metadata and the embedding payload
5. The in-memory vector index is updated for fast similarity search
6. Duplicate detection and contradiction detection can compare the new entry against existing vector matches

Stored memory includes:

- `Content`
- `Type`
- `Namespace`
- `Tags`
- `CreatedAt`
- `UpdatedAt`
- `LastAccessedAt`
- optional `ExpiresAt`
- optional version history
- optional links to other memories

### Search Flow

There are two search modes:

- **Full-text search** uses SQL filtering on stored content and metadata
- **Semantic search** embeds the query text and compares it with stored vectors using cosine similarity

For semantic search:

1. The query text is converted into an embedding
2. The vector index returns the nearest memory IDs with similarity scores
3. The repository loads the matching entries from SQLite
4. Namespace, type, tag, and date filters are applied
5. Results are returned with relevance scores

This design separates **metadata storage** from **vector similarity lookup**:

- SQLite is the source of truth
- the in-memory vector store is the fast index used for ranking

## Why AI Is Used

AI is used for two distinct jobs:

### 1. Embeddings

Embeddings convert text into numeric vectors. These vectors are used for:

- semantic search
- near-duplicate detection
- contradiction candidate detection

Embeddings do not generate text. They only produce vectors.

### 2. Summarization

Summarization converts long conversation text into compact notes. This is used by `summarize_conversation` and can optionally store the summary as a memory entry.

Summarization generates text. It does not produce vectors unless that summary is later stored and embedded.

## `Embedding` vs `AI` Configuration

The config has two separate sections because they solve different problems.

### `Embedding`

`Embedding` controls the provider used for vector generation.

It affects:

- `store_memory`
- `update_memory`
- `search_memories` in semantic mode
- deduplication
- contradiction detection

Example:

```json
"Embedding": {
  "Provider": "GapGpt",
  "Model": "text-embedding-3-small",
  "BaseUrl": "https://api.gapgpt.app/v1",
  "ApiKey": "",
  "Dimensions": 768
}
```

### `AI`

`AI` controls the provider used for text-generation tasks.

Right now it affects:

- `summarize_conversation`

Example:

```json
"AI": {
  "Provider": "GapGpt",
  "Model": "gapgpt-qwen-3.5",
  "BaseUrl": "https://api.gapgpt.app/v1",
  "ApiKey": ""
}
```

### Why They Are Separate

This split is intentional:

- an embedding model is usually not the same as a text-generation model
- you may want GapGPT for summaries but Ollama for local embeddings
- you may want remote embeddings but no remote text generation
- failures and costs are different for each kind of workload

## Supported Provider Modes

### Embedding Providers

- `Ollama` uses the Ollama embedding API
- `GapGpt` uses an OpenAI-compatible embedding endpoint
- fallback deterministic local embeddings are used if Ollama is selected but unavailable

### Summarization Providers

- `Simple` uses a local heuristic summarizer with no external API
- `GapGpt` uses the OpenAI-compatible Responses API

## GapGPT / OpenAI-Compatible Usage

To use a custom base URL such as `https://api.gapgpt.app/v1`, configure both sections explicitly.

```json
"Embedding": {
  "Provider": "GapGpt",
  "Model": "text-embedding-3-small",
  "BaseUrl": "https://api.gapgpt.app/v1",
  "ApiKey": "",
  "Dimensions": 768
},
"AI": {
  "Provider": "GapGpt",
  "Model": "gapgpt-qwen-3.5",
  "BaseUrl": "https://api.gapgpt.app/v1",
  "ApiKey": ""
}
```

Use environment variables instead of committing secrets:

```bash
export MEMORYMCP_AI_API_KEY="your-gapgpt-key"
export MEMORYMCP_EMBEDDING_API_KEY="your-gapgpt-key"
```

If your provider does not expose embeddings, keep:

- `Embedding.Provider = "Ollama"`
- `AI.Provider = "GapGpt"`

That is a valid mixed mode.

## Persistence Model

The source of truth is SQLite. Each memory entry is persisted with:

- core metadata
- tags
- version history
- outgoing links
- serialized embedding bytes

At startup, the server loads stored embeddings into the in-memory vector index. If the process restarts, the vector index is rebuilt from SQLite automatically.

## Versioning and Links

### Versioning

When content changes:

- the old content is copied into `MemoryVersion`
- the current entry is updated
- timestamps are refreshed

This preserves edit history for later inspection or future rollback support.

### Links

Memories can be connected directionally, for example:

- `RelatedTo`
- `DerivedFrom`
- `Contradicts`
- `Supersedes`

These links make it possible to represent relationships between stored facts or instructions.

## Expiration and Staleness

The server distinguishes two concepts:

- **Expired** means the memory passed its `ExpiresAt` timestamp
- **Stale** means the memory has not been accessed for a long time

Relevant tools:

- `cleanup_expired_memories`
- `find_stale_memories`

## Main MCP Tools

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

## Run

```bash
dotnet run --project src/MemoryMcp.Server
```

The server uses stdio transport and reads configuration from `src/MemoryMcp.Server/appsettings.json`.

## Validation

```bash
dotnet build MemoryMcp.slnx
dotnet test MemoryMcp.slnx
```
