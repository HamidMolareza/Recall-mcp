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

    [McpServerTool(Name = "rebuild_vector_index"), Description("Rebuilds the in-memory vector index from persisted embeddings in SQLite and can optionally re-generate embeddings for all memories using the configured provider/model.")]
    public async Task<object> RebuildVectorIndexAsync(
        [Description("When true, re-generates embeddings for all persisted memories before rebuilding the in-memory vector index.")]
        bool regenerateEmbeddings = false,
        CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("rebuild_vector_index", _logger, _operationNoticeSink, async innerCt =>
        {
            var rebuild = await _vectorIndexMaintenanceService.RebuildAsync(regenerateEmbeddings, innerCt);

            _logger.LogInformation(
                "{IsAudit} Rebuilt the in-memory vector index from {PersistedEmbeddingCount} persisted embeddings; re-generated {ReembeddedMemoryCount} embeddings",
                true,
                rebuild.PersistedEmbeddingCount,
                rebuild.ReembeddedMemoryCount);

            return new RebuildVectorIndexResult(
                rebuild.PersistedEmbeddingCount,
                rebuild.ReembeddedMemoryCount,
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
    int ReembeddedMemoryCount,
    RuntimeVectorIndexStatus VectorIndex);
