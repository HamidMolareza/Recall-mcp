using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Application.Services;

public sealed class StalenessService
{
    private readonly IMemoryRepository _repository;
    private readonly ILogger<StalenessService> _logger;

    public StalenessService(
        IMemoryRepository repository,
        ILogger<StalenessService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<IReadOnlyList<MemoryResponse>> FindStaleAsync(int thresholdDays, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> CleanupExpiredAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
