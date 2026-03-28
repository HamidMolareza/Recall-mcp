using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure.Export;

public sealed class JsonExportService : IExportService
{
    private readonly ILogger<JsonExportService> _logger;

    public JsonExportService(ILogger<JsonExportService> logger)
    {
        _logger = logger;
    }

    public Task<string> ExportToJsonAsync(IEnumerable<MemoryEntry> entries, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> ExportToMarkdownAsync(IEnumerable<MemoryEntry> entries, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<MemoryEntry>> ImportFromJsonAsync(string json, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
