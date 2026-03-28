using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class WipeNamespaceTool
{
    private readonly MemoryService _memoryService;
    private readonly StalenessService _stalenessService;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<WipeNamespaceTool> _logger;

    public WipeNamespaceTool(
        MemoryService memoryService,
        StalenessService stalenessService,
        IOperationNoticeSink operationNoticeSink,
        ILogger<WipeNamespaceTool> logger)
    {
        _memoryService = memoryService;
        _stalenessService = stalenessService;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "wipe_namespace"), Description("Deletes all memories in a namespace hierarchy.")]
    public async Task<object> WipeNamespaceAsync(string memoryNamespace, CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("wipe_namespace", _logger, _operationNoticeSink, async innerCt =>
        {
            await _memoryService.WipeNamespaceAsync(memoryNamespace, innerCt);
            _logger.LogWarning("Wiped namespace {Namespace}", memoryNamespace);
            return new { wiped = true, memoryNamespace = memoryNamespace.Trim().ToLowerInvariant() };
        }, ct);
    }

    [McpServerTool(Name = "cleanup_expired_memories"), Description("Deletes expired memories from storage.")]
    public async Task<object> CleanupExpiredMemoriesAsync(CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("cleanup_expired_memories", _logger, _operationNoticeSink, async innerCt =>
        {
            var deletedCount = await _stalenessService.CleanupExpiredAsync(innerCt);
            return new { deletedCount };
        }, ct);
    }
}
