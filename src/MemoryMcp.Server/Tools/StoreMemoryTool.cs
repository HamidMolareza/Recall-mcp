using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Enums;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class StoreMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly DeduplicationService _deduplicationService;
    private readonly ContradictionDetector _contradictionDetector;
    private readonly ILogger<StoreMemoryTool> _logger;

    public StoreMemoryTool(
        MemoryService memoryService,
        DeduplicationService deduplicationService,
        ContradictionDetector contradictionDetector,
        ILogger<StoreMemoryTool> logger)
    {
        _memoryService = memoryService;
        _deduplicationService = deduplicationService;
        _contradictionDetector = contradictionDetector;
        _logger = logger;
    }

    [McpServerTool(Name = "store_memory"), Description("Stores a memory entry and reports duplicates or contradictions.")]
    public async Task<object> StoreMemoryAsync(
        string content,
        MemoryType type,
        string memoryNamespace,
        string[]? tags = null,
        DateTime? expiresAt = null,
        bool rejectDuplicates = false,
        CancellationToken ct = default)
    {
        var duplicateIds = await _deduplicationService.FindDuplicatesAsync(content, ct: ct);
        if (rejectDuplicates && duplicateIds.Count > 0)
        {
            throw new MemoryMcp.Domain.Exceptions.DuplicateMemoryException(duplicateIds[0]);
        }

        var contradictionIds = await _contradictionDetector.DetectContradictionsAsync(content, memoryNamespace, ct);
        var memory = await _memoryService.StoreAsync(new StoreMemoryRequest
        {
            Content = content,
            Type = type,
            Namespace = memoryNamespace,
            Tags = tags,
            ExpiresAt = expiresAt
        }, ct);

        _logger.LogInformation("Stored memory {MemoryId}", memory.Id);
        return new
        {
            memory,
            duplicates = duplicateIds,
            contradictions = contradictionIds
        };
    }
}
