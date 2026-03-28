using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class UpdateMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<UpdateMemoryTool> _logger;

    public UpdateMemoryTool(MemoryService memoryService, ILogger<UpdateMemoryTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }
}
