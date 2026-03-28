# MemoryMcp Tool Guide

## Tool Matrix

- `store_memory`: Create a new memory entry with content, namespace, type, tags, expiration, and optional duplicate handling.
- `get_memory`: Fetch one memory by ID.
- `list_memories`: Browse memories with exact filters such as namespace, tags, type, date range, and paging.
- `search_memories`: Run semantic or full-text retrieval; use semantic mode when recall depends on meaning instead of exact words.
- `update_memory`: Modify an existing memory while keeping its identity.
- `link_memories`: Create a directional relationship between two memories.
- `delete_memory`: Remove one memory by ID.
- `summarize_conversation`: Turn long text into a compact summary and optionally store it as memory.
- `export_memories`: Dump memories in JSON or Markdown for backup or transfer.
- `import_memories`: Load exported memories back into the store.
- `find_stale_memories`: Identify memories that have not been accessed recently.
- `cleanup_expired_memories`: Delete memories whose expiration timestamp has passed.
- `wipe_namespace`: Remove all memories under a namespace hierarchy.

## Retrieval Strategy

- Use `search_memories` first when the user asks what was remembered about a topic, preference, or prior discussion.
- Use `list_memories` first when the namespace is known and you need controlled inspection instead of semantic ranking.
- Use `get_memory` only after another tool already gave you a memory ID.

## Writing Strategy

- Prefer `update_memory` over `store_memory` when the new content is clearly a revision of the same fact.
- Add tags only when they improve later filtering.
- Link memories when the relationship itself will matter later, not just because two entries mention similar topics.

## Namespace Patterns

- `user/preferences`: stable personal preferences and defaults.
- `user/profile`: stable facts about the user that help future assistance.
- `project/context`: architecture, setup, constraints, and current direction.
- `project/feature-name`: feature-specific implementation details and decisions.
- `team/topic`: shared operational notes across collaborators.
- `qa/session-name`: temporary test data that can be wiped later.

## Response Handling

- Successful operations may still include `warnings`.
- Provider warnings usually mean external AI failed and the server used a local fallback.
- Report user-actionable warnings when they affect result quality, such as quota exhaustion, authentication problems, or network failures.
- Do not treat fallback warnings as storage corruption unless the tool explicitly reports an error.

## Example Triggers

- "Remember that I prefer SQLite for local tools."
- "What do you already know about this project?"
- "Summarize this conversation and keep it for later."
- "Find stale notes in the `project/context` namespace."
- "Export everything under `user/preferences`."
