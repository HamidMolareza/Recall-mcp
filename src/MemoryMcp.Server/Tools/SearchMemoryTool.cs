using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class SearchMemoryTool
{
    private readonly SearchService _searchService;
    private readonly ILogger<SearchMemoryTool> _logger;

    public SearchMemoryTool(SearchService searchService, ILogger<SearchMemoryTool> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }
}
