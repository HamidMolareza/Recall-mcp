using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class DeleteMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<DeleteMemoryTool> _logger;

    public DeleteMemoryTool(MemoryService memoryService, ILogger<DeleteMemoryTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }
}
