using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class GetMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<GetMemoryTool> _logger;

    public GetMemoryTool(MemoryService memoryService, IOperationNoticeSink operationNoticeSink, ILogger<GetMemoryTool> logger)
    {
        _memoryService = memoryService;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "get_memory"), Description("Gets a memory entry by id.")]
    public async Task<object> GetMemoryAsync(Guid id, CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("get_memory", _logger, _operationNoticeSink, async innerCt =>
        {
            var memory = await _memoryService.GetByIdAsync(id, innerCt);
            _logger.LogInformation("Loaded memory {MemoryId}", id);
            return memory;
        }, ct);
    }
}
