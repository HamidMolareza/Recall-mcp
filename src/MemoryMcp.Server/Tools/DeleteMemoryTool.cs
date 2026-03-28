using MemoryMcp.Application.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class DeleteMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<DeleteMemoryTool> _logger;

    public DeleteMemoryTool(MemoryService memoryService, ILogger<DeleteMemoryTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    [McpServerTool(Name = "delete_memory"), Description("Deletes a memory entry by id.")]
    public async Task<object> DeleteMemoryAsync(Guid id, CancellationToken ct = default)
    {
        await _memoryService.DeleteAsync(id, ct);
        _logger.LogInformation("Deleted memory {MemoryId}", id);
        return new { deleted = true, id };
    }
}
