using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Application.DTOs;

public sealed record SearchMemoryRequest
{
    public string? Query { get; init; }
    public string? Namespace { get; init; }
    public MemoryType? Type { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int TopK { get; init; } = 10;
    public bool SemanticSearch { get; init; } = true;
}
