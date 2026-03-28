using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Application.DTOs;

public sealed record StoreMemoryRequest
{
    public required string Content { get; init; }
    public required MemoryType Type { get; init; }
    public required string Namespace { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
