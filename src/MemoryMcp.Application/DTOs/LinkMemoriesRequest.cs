using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Application.DTOs;

public sealed record LinkMemoriesRequest
{
    public required Guid SourceId { get; init; }
    public required Guid TargetId { get; init; }
    public required LinkRelation Relation { get; init; }
    public string? Label { get; init; }
}
