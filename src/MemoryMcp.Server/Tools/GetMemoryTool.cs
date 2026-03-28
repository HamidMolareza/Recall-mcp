using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class GetMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<GetMemoryTool> _logger;

    public GetMemoryTool(MemoryService memoryService, ILogger<GetMemoryTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }
}
