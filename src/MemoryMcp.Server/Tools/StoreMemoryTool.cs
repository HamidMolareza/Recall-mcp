using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
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
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<StoreMemoryTool> _logger;

    public StoreMemoryTool(
        MemoryService memoryService,
        DeduplicationService deduplicationService,
        ContradictionDetector contradictionDetector,
        IOperationNoticeSink operationNoticeSink,
        ILogger<StoreMemoryTool> logger)
    {
        _memoryService = memoryService;
        _deduplicationService = deduplicationService;
        _contradictionDetector = contradictionDetector;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "store_memory"), Description("Stores a memory entry and reports duplicates or contradictions.")]
    public async Task<object> StoreMemoryAsync(
        string content,
        [Description("Memory type for the new entry. " + MemoryTypeArgumentParser.AllowedValuesDescription)]
        string type,
        string memoryNamespace,
        string[]? tags = null,
        DateTime? expiresAt = null,
        bool rejectDuplicates = false,
        CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("store_memory", _logger, _operationNoticeSink, async innerCt =>
        {
            var parsedType = MemoryTypeArgumentParser.ParseRequired(type, nameof(type));
            var duplicateIds = await _deduplicationService.FindDuplicatesAsync(content, ct: innerCt);
            if (rejectDuplicates && duplicateIds.Count > 0)
            {
                throw new MemoryMcp.Domain.Exceptions.DuplicateMemoryException(duplicateIds[0]);
            }

            var contradictionIds = await _contradictionDetector.DetectContradictionsAsync(content, memoryNamespace, innerCt);
            var memory = await _memoryService.StoreAsync(new StoreMemoryRequest
            {
                Content = content,
                Type = parsedType,
                Namespace = memoryNamespace,
                Tags = tags,
                ExpiresAt = expiresAt
            }, innerCt);

            _logger.LogInformation("Stored memory {MemoryId}", memory.Id);
            return new
            {
                memory,
                duplicates = duplicateIds,
                contradictions = contradictionIds
            };
        }, ct);
    }
}
