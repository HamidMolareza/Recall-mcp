using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class ListMemoriesTool
{
    private readonly MemoryService _memoryService;
    private readonly StalenessService _stalenessService;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<ListMemoriesTool> _logger;

    public ListMemoriesTool(
        MemoryService memoryService,
        StalenessService stalenessService,
        IOperationNoticeSink operationNoticeSink,
        ILogger<ListMemoriesTool> logger)
    {
        _memoryService = memoryService;
        _stalenessService = stalenessService;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "list_memories"), Description("Lists memories with filtering and pagination.")]
    public async Task<object> ListMemoriesAsync(
        string? memoryNamespace = null,
        [Description("Optional memory type filter. " + MemoryTypeArgumentParser.AllowedValuesDescription)]
        string? type = null,
        string[]? tags = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchText = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("list_memories", _logger, _operationNoticeSink, async innerCt =>
        {
            var parsedType = MemoryTypeArgumentParser.ParseOptional(type, nameof(type));
            var result = await _memoryService.ListAsync(new MemoryQueryFilter
            {
                Namespace = memoryNamespace,
                Type = parsedType,
                Tags = tags,
                FromDate = fromDate,
                ToDate = toDate,
                SearchText = searchText,
                Page = page,
                PageSize = pageSize
            }, innerCt);

            _logger.LogInformation("Listed memories page {Page}", result.Page);
            return result;
        }, ct);
    }

    [McpServerTool(Name = "find_stale_memories"), Description("Finds stale memories that have not been accessed recently.")]
    public async Task<object> FindStaleMemoriesAsync(int thresholdDays = 90, CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("find_stale_memories", _logger, _operationNoticeSink, async innerCt =>
        {
            var stale = await _stalenessService.FindStaleAsync(thresholdDays, innerCt);
            return new
            {
                thresholdDays,
                count = stale.Count,
                items = stale
            };
        }, ct);
    }
}
