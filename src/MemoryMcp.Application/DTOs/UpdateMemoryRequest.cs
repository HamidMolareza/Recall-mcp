using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Application.DTOs;

public sealed record UpdateMemoryRequest
{
    public required Guid Id { get; init; }
    public string? Content { get; init; }
    public MemoryType? Type { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool UpdateExpiresAt { get; init; }
    public EmbeddingVector? Embedding { get; init; }
}
