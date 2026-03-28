using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class WipeNamespaceTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<WipeNamespaceTool> _logger;

    public WipeNamespaceTool(MemoryService memoryService, ILogger<WipeNamespaceTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }
}
