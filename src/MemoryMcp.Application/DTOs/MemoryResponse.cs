using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Application.DTOs;

public sealed record MemoryResponse
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required MemoryType Type { get; init; }
    public required string Namespace { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required DateTime LastAccessedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public double? RelevanceScore { get; init; }
}
