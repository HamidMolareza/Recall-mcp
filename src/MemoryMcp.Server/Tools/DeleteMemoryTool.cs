using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class DeleteMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<DeleteMemoryTool> _logger;

    public DeleteMemoryTool(MemoryService memoryService, IOperationNoticeSink operationNoticeSink, ILogger<DeleteMemoryTool> logger)
    {
        _memoryService = memoryService;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "delete_memory"), Description("Deletes a memory entry by id.")]
    public async Task<object> DeleteMemoryAsync(Guid id, CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("delete_memory", _logger, _operationNoticeSink, async innerCt =>
        {
            await _memoryService.DeleteAsync(id, innerCt);
            _logger.LogInformation("Deleted memory {MemoryId}", id);
            return new { deleted = true, id };
        }, ct);
    }
}
