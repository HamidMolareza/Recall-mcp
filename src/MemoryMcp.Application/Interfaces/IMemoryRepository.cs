using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Application.Interfaces;

public interface IMemoryRepository
{
    Task<MemoryEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryEntry>> ListAsync(MemoryQueryFilter filter, CancellationToken ct = default);
    Task<int> CountAsync(MemoryQueryFilter filter, CancellationToken ct = default);
    Task AddAsync(MemoryEntry entry, CancellationToken ct = default);
    Task UpdateAsync(MemoryEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteByNamespaceAsync(string ns, CancellationToken ct = default);
}

public sealed record MemoryQueryFilter
{
    public string? Namespace { get; init; }
    public MemoryType? Type { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? SearchText { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
