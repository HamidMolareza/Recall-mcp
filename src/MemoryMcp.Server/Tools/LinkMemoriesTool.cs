using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class LinkMemoriesTool
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<LinkMemoriesTool> _logger;

    public LinkMemoriesTool(MemoryService memoryService, ILogger<LinkMemoriesTool> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }
}
