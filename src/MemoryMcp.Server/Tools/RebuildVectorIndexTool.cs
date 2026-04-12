using MemoryMcp.Application.Interfaces;
using MemoryMcp.Infrastructure.VectorSearch;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class RebuildVectorIndexTool
{
    private readonly VectorIndexMaintenanceService _vectorIndexMaintenanceService;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<RebuildVectorIndexTool> _logger;

    public RebuildVectorIndexTool(
        VectorIndexMaintenanceService vectorIndexMaintenanceService,
        IOperationNoticeSink operationNoticeSink,
        ILogger<RebuildVectorIndexTool> logger)
    {
        _vectorIndexMaintenanceService = vectorIndexMaintenanceService;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "rebuild_vector_index"), Description("Rebuilds the in-memory vector index from persisted embeddings in SQLite.")]
    public async Task<object> RebuildVectorIndexAsync(CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("rebuild_vector_index", _logger, _operationNoticeSink, async innerCt =>
        {
            var rebuild = await _vectorIndexMaintenanceService.RebuildAsync(innerCt);

            _logger.LogInformation(
                "{IsAudit} Rebuilt the in-memory vector index from {PersistedEmbeddingCount} persisted embeddings",
                true,
                rebuild.PersistedEmbeddingCount);

            return new RebuildVectorIndexResult(
                rebuild.PersistedEmbeddingCount,
                new RuntimeVectorIndexStatus(
                    rebuild.Snapshot.EntryCount,
                    rebuild.Snapshot.NamespacedEntryCount,
                    rebuild.Snapshot.UnscopedEntryCount,
                    rebuild.Snapshot.NamespaceCounts));
        }, ct);
    }
}

public sealed record RebuildVectorIndexResult(
    int PersistedEmbeddingCount,
    RuntimeVectorIndexStatus VectorIndex);
