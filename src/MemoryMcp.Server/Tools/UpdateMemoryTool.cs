using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Enums;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class UpdateMemoryTool
{
    private readonly MemoryService _memoryService;
    private readonly DeduplicationService _deduplicationService;
    private readonly ContradictionDetector _contradictionDetector;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<UpdateMemoryTool> _logger;

    public UpdateMemoryTool(
        MemoryService memoryService,
        DeduplicationService deduplicationService,
        ContradictionDetector contradictionDetector,
        IOperationNoticeSink operationNoticeSink,
        ILogger<UpdateMemoryTool> logger)
    {
        _memoryService = memoryService;
        _deduplicationService = deduplicationService;
        _contradictionDetector = contradictionDetector;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "update_memory"), Description("Updates an existing memory entry and reports duplicates or contradictions for new content.")]
    public async Task<object> UpdateMemoryAsync(
        Guid id,
        string? content = null,
        MemoryType? type = null,
        string[]? tags = null,
        DateTime? expiresAt = null,
        CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("update_memory", _logger, _operationNoticeSink, async innerCt =>
        {
            IReadOnlyList<Guid> duplicateIds = [];
            IReadOnlyList<Guid> contradictionIds = [];

            if (!string.IsNullOrWhiteSpace(content))
            {
                duplicateIds = await _deduplicationService.FindDuplicatesAsync(content, ct: innerCt);
                var current = await _memoryService.GetByIdAsync(id, innerCt);
                contradictionIds = await _contradictionDetector.DetectContradictionsAsync(content, current.Namespace, innerCt);
            }

            var memory = await _memoryService.UpdateAsync(new UpdateMemoryRequest
            {
                Id = id,
                Content = content,
                Type = type,
                Tags = tags,
                ExpiresAt = expiresAt
            }, innerCt);

            _logger.LogInformation("Updated memory {MemoryId}", id);
            return new
            {
                memory,
                duplicates = duplicateIds.Where(candidateId => candidateId != id).ToArray(),
                contradictions = contradictionIds.Where(candidateId => candidateId != id).ToArray()
            };
        }, ct);
    }
}
