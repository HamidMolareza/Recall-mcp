using MemoryMcp.Application.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class GetMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<GetMemoryTool> _logger;

    public GetMemoryTool(MemoryService memoryService, ILogger<GetMemoryTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    [McpServerTool(Name = "get_memory"), Description("Gets a memory entry by id.")]
    public async Task<object> GetMemoryAsync(Guid id, CancellationToken ct = default)
    {
        var memory = await _memoryService.GetByIdAsync(id, ct);
        _logger.LogInformation("Loaded memory {MemoryId}", id);
        return memory;
    }
}
