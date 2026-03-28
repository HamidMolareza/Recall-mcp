using MemoryMcp.Domain.Entities;

namespace MemoryMcp.Application.Interfaces;

public interface IExportService
{
    Task<string> ExportToJsonAsync(IEnumerable<MemoryEntry> entries, CancellationToken ct = default);
    Task<string> ExportToMarkdownAsync(IEnumerable<MemoryEntry> entries, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryEntry>> ImportFromJsonAsync(string json, CancellationToken ct = default);
}
