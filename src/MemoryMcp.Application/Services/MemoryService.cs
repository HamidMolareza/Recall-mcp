using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Application.Services;

public sealed class MemoryService
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(
        IMemoryRepository repository,
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        ILogger<MemoryService> logger)
    {
        _repository = repository;
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public Task<MemoryResponse> StoreAsync(StoreMemoryRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<MemoryResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<MemoryResponse> UpdateAsync(UpdateMemoryRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedList<MemoryResponse>> ListAsync(MemoryQueryFilter filter, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task WipeNamespaceAsync(string ns, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task LinkAsync(LinkMemoriesRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
