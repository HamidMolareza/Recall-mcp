namespace MemoryMcp.Application.DTOs;

public sealed record SearchResult
{
    public required MemoryResponse Memory { get; init; }
    public required double Score { get; init; }
}
