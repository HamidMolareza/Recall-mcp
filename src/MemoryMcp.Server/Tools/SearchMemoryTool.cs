using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Enums;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class SearchMemoryTool
{
    private readonly SearchService _searchService;
    private readonly ILogger<SearchMemoryTool> _logger;

    public SearchMemoryTool(SearchService searchService, ILogger<SearchMemoryTool> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [McpServerTool(Name = "search_memories"), Description("Searches memories using semantic or full-text retrieval.")]
    public async Task<object> SearchMemoriesAsync(
        string query,
        bool semanticSearch = true,
        string? memoryNamespace = null,
        MemoryType? type = null,
        string[]? tags = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int topK = 10,
        CancellationToken ct = default)
    {
        if (semanticSearch)
        {
            var semanticResults = await _searchService.SemanticSearchAsync(new SearchMemoryRequest
            {
                Query = query,
                SemanticSearch = true,
                Namespace = memoryNamespace,
                Type = type,
                Tags = tags,
                FromDate = fromDate,
                ToDate = toDate,
                TopK = topK
            }, ct);

            return new
            {
                mode = "semantic",
                count = semanticResults.Count,
                items = semanticResults
            };
        }

        var fullTextResults = await _searchService.FullTextSearchAsync(query, topK, ct);
        return new
        {
            mode = "full-text",
            count = fullTextResults.Count,
            items = fullTextResults
        };
    }
}
