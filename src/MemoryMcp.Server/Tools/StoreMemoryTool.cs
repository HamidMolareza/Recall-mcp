using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class StoreMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<StoreMemoryTool> _logger;

    public StoreMemoryTool(MemoryService memoryService, ILogger<StoreMemoryTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }
}
