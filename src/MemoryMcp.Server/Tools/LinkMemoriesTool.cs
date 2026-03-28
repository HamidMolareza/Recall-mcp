using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Application.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class LinkMemoriesTool
{
    private readonly MemoryService _memoryService;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<LinkMemoriesTool> _logger;

    public LinkMemoriesTool(MemoryService memoryService, IOperationNoticeSink operationNoticeSink, ILogger<LinkMemoriesTool> logger)
    {
        _memoryService = memoryService;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "link_memories"), Description("Creates a directional relationship between two memories.")]
    public async Task<object> LinkMemoriesAsync(
        Guid sourceId,
        Guid targetId,
        LinkRelation relation,
        string? label = null,
        CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("link_memories", _logger, _operationNoticeSink, async innerCt =>
        {
            await _memoryService.LinkAsync(new LinkMemoriesRequest
            {
                SourceId = sourceId,
                TargetId = targetId,
                Relation = relation,
                Label = label
            }, innerCt);

            _logger.LogInformation("Linked memory {SourceId} to {TargetId}", sourceId, targetId);
            return new { linked = true, sourceId, targetId, relation, label };
        }, ct);
    }
}
