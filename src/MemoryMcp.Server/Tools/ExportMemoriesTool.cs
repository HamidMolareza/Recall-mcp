using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class ExportMemoriesTool
{
    private readonly IExportService _exportService;
    private readonly IMemoryRepository _repository;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<ExportMemoriesTool> _logger;

    public ExportMemoriesTool(
        IExportService exportService,
        IMemoryRepository repository,
        IVectorStore vectorStore,
        ILogger<ExportMemoriesTool> logger)
    {
        _exportService = exportService;
        _repository = repository;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    [McpServerTool(Name = "export_memories"), Description("Exports memories in JSON or Markdown format.")]
    public async Task<object> ExportMemoriesAsync(
        string format = "json",
        string? memoryNamespace = null,
        string? query = null,
        int maxItems = 1000,
        CancellationToken ct = default)
    {
        var entries = await _repository.ListAsync(new MemoryQueryFilter
        {
            Namespace = memoryNamespace,
            SearchText = query,
            Page = 1,
            PageSize = Math.Clamp(maxItems, 1, 5000)
        }, ct);

        var payload = string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase)
            ? await _exportService.ExportToMarkdownAsync(entries, ct)
            : await _exportService.ExportToJsonAsync(entries, ct);

        _logger.LogInformation("Exported {Count} memories as {Format}", entries.Count, format);
        return new
        {
            format = format.ToLowerInvariant(),
            count = entries.Count,
            content = payload
        };
    }

    [McpServerTool(Name = "import_memories"), Description("Imports memories from exported JSON.")]
    public async Task<object> ImportMemoriesAsync(string json, bool skipExisting = true, CancellationToken ct = default)
    {
        var entries = await _exportService.ImportFromJsonAsync(json, ct);
        var imported = new List<Guid>();
        var skipped = new List<Guid>();

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();

            var existing = await _repository.GetByIdAsync(entry.Id, ct);
            if (existing is not null)
            {
                if (skipExisting)
                {
                    skipped.Add(entry.Id);
                    continue;
                }

                await _repository.DeleteAsync(entry.Id, ct);
                await _vectorStore.DeleteAsync(entry.Id, ct);
            }

            await _repository.AddAsync(entry, ct);
            if (entry.Embedding is not null)
            {
                await _vectorStore.UpsertAsync(entry.Id, entry.Embedding, entry.Namespace.Value, ct);
            }

            imported.Add(entry.Id);
        }

        _logger.LogInformation("Imported {ImportedCount} memories", imported.Count);
        return new
        {
            imported,
            skipped
        };
    }
}
