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
    private readonly ILogger<WipeNamespaceTool> _logger;

    public WipeNamespaceTool(
        MemoryService memoryService,
        StalenessService stalenessService,
        ILogger<WipeNamespaceTool> logger)
    {
        _memoryService = memoryService;
        _stalenessService = stalenessService;
        _logger = logger;
    }

    [McpServerTool(Name = "wipe_namespace"), Description("Deletes all memories in a namespace hierarchy.")]
    public async Task<object> WipeNamespaceAsync(string memoryNamespace, CancellationToken ct = default)
    {
        await _memoryService.WipeNamespaceAsync(memoryNamespace, ct);
        _logger.LogWarning("Wiped namespace {Namespace}", memoryNamespace);
        return new { wiped = true, memoryNamespace = memoryNamespace.Trim().ToLowerInvariant() };
    }

    [McpServerTool(Name = "cleanup_expired_memories"), Description("Deletes expired memories from storage.")]
    public async Task<object> CleanupExpiredMemoriesAsync(CancellationToken ct = default)
    {
        var deletedCount = await _stalenessService.CleanupExpiredAsync(ct);
        return new { deletedCount };
    }
}
