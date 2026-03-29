using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class SearchMemoryTool
{
    private readonly SearchService _searchService;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<SearchMemoryTool> _logger;

    public SearchMemoryTool(SearchService searchService, IOperationNoticeSink operationNoticeSink, ILogger<SearchMemoryTool> logger)
    {
        _searchService = searchService;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "search_memories"), Description("Searches memories using semantic or full-text retrieval.")]
    public async Task<object> SearchMemoriesAsync(
        string query,
        bool semanticSearch = true,
        string? memoryNamespace = null,
        [Description("Optional memory type filter. " + MemoryTypeArgumentParser.AllowedValuesDescription)]
        string? type = null,
        string[]? tags = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int topK = 10,
        CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("search_memories", _logger, _operationNoticeSink, async innerCt =>
        {
            var parsedType = MemoryTypeArgumentParser.ParseOptional(type, nameof(type));
            if (semanticSearch)
            {
                var semanticResults = await _searchService.SemanticSearchAsync(new SearchMemoryRequest
                {
                    Query = query,
                    SemanticSearch = true,
                    Namespace = memoryNamespace,
                    Type = parsedType,
                    Tags = tags,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TopK = topK
                }, innerCt);

                return new
                {
                    mode = "semantic",
                    count = semanticResults.Count,
                    items = semanticResults
                };
            }

            var fullTextResults = await _searchService.FullTextSearchAsync(query, topK, innerCt);
            return new
            {
                mode = "full-text",
                count = fullTextResults.Count,
                items = fullTextResults
            };
        }, ct);
    }
}
