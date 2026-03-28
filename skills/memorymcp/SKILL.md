---
name: memorymcp
description: "Use when Codex should work with the configured memorymcp MCP server for durable memory: storing stable facts, preferences, instructions, project context, and summaries; searching prior memory by meaning or filters; updating, linking, exporting, importing, or cleaning memory namespaces; or deciding whether information should be remembered across sessions."
---

# MemoryMcp

Use the configured `memorymcp` MCP server as the durable memory layer for Codex. Treat it as long-term structured recall, not as a scratchpad for every transient chat detail.

## Work Deliberately

- Prefer reading before writing when the user may already have stored the fact, preference, or project context.
- Store only information that is likely to matter beyond the current turn: durable user preferences, project facts, decisions, constraints, summaries, or references worth recalling later.
- Keep namespaces intentional. Prefer patterns such as `user/preferences`, `project/context`, `project/feature-x`, or `team/topic`.
- Avoid storing secrets unless the user explicitly asks for that behavior and the surrounding environment is trusted.

## Choose The Right Tool

- Use `search_memories` for semantic recall when only the meaning is known.
- Use `list_memories` for exact browsing by namespace, type, tag, date, or pagination.
- Use `get_memory` when the memory ID is already known.
- Use `store_memory` to create new durable memory.
- Use `update_memory` to revise existing memory instead of creating near-duplicates.
- Use `link_memories` to connect related items such as `DerivedFrom`, `RelatedTo`, `Supersedes`, or `Contradicts`.
- Use `summarize_conversation` to compress long text into a reusable note, then store it when continued recall is useful.
- Use `find_stale_memories`, `cleanup_expired_memories`, and `wipe_namespace` for maintenance.
- Use `export_memories` and `import_memories` for backup, migration, or bulk loading.

Read `references/tool-guide.md` when you need the detailed tool matrix, response handling rules, or namespace patterns.

## Handle Results Correctly

- Treat a response with successful data plus `warnings` as a degraded success, not a hard failure.
- Expect embedding or summarization providers to fall back locally when external AI calls fail.
- If the tool returns an error object, surface the clear `message` and any suggested remediation to the user instead of hiding the failure.
- When storage succeeds with a provider warning, continue the task and mention the warning only if it changes quality, freshness, or expected behavior.

## Respect Provider Roles

- Treat `Embedding` configuration as the vector path for semantic search, deduplication, contradiction checks, and memory writes that need embeddings.
- Treat `AI` configuration as the text-generation path for `summarize_conversation`.
- Do not assume the same model or provider should be used for both roles.

## Suggested Workflow

1. Decide whether the user needs recall, search, summarization, or maintenance.
2. Choose or confirm a namespace strategy before writing.
3. Search or list first when duplicates are plausible.
4. Store, update, or link memories with clear content and metadata.
5. Reuse retrieved memory in the answer and mention important warnings or gaps when they matter.
