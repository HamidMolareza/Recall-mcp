using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Enums;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class ListMemoriesTool
{
    private readonly MemoryService _memoryService;
    private readonly StalenessService _stalenessService;
    private readonly ILogger<ListMemoriesTool> _logger;

    public ListMemoriesTool(
        MemoryService memoryService,
        StalenessService stalenessService,
        ILogger<ListMemoriesTool> logger)
    {
        _memoryService = memoryService;
        _stalenessService = stalenessService;
        _logger = logger;
    }

    [McpServerTool(Name = "list_memories"), Description("Lists memories with filtering and pagination.")]
    public async Task<object> ListMemoriesAsync(
        string? memoryNamespace = null,
        MemoryType? type = null,
        string[]? tags = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchText = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _memoryService.ListAsync(new MemoryQueryFilter
        {
            Namespace = memoryNamespace,
            Type = type,
            Tags = tags,
            FromDate = fromDate,
            ToDate = toDate,
            SearchText = searchText,
            Page = page,
            PageSize = pageSize
        }, ct);

        _logger.LogInformation("Listed memories page {Page}", result.Page);
        return result;
    }

    [McpServerTool(Name = "find_stale_memories"), Description("Finds stale memories that have not been accessed recently.")]
    public async Task<object> FindStaleMemoriesAsync(int thresholdDays = 90, CancellationToken ct = default)
    {
        var stale = await _stalenessService.FindStaleAsync(thresholdDays, ct);
        return new
        {
            thresholdDays,
            count = stale.Count,
            items = stale
        };
    }
}
