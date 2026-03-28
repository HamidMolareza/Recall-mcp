using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure.Persistence.Repositories;

public sealed class MemoryRepository : IMemoryRepository
{
    private readonly MemoryDbContext _dbContext;
    private readonly ILogger<MemoryRepository> _logger;

    public MemoryRepository(MemoryDbContext dbContext, ILogger<MemoryRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<MemoryEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<MemoryEntry>> ListAsync(MemoryQueryFilter filter, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync(MemoryQueryFilter filter, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(MemoryEntry entry, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(MemoryEntry entry, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteByNamespaceAsync(string ns, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
