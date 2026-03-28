using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class ListMemoriesTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<ListMemoriesTool> _logger;

    public ListMemoriesTool(MemoryService memoryService, ILogger<ListMemoriesTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }
}
