using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Application.DTOs;

public sealed record UpdateMemoryRequest
{
    public required Guid Id { get; init; }
    public string? Content { get; init; }
    public MemoryType? Type { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
