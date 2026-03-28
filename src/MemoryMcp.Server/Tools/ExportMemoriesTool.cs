using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class ExportMemoriesTool
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportMemoriesTool> _logger;

    public ExportMemoriesTool(IExportService exportService, ILogger<ExportMemoriesTool> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }
}
