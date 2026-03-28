using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Exceptions;
using MemoryMcp.Domain.ValueObjects;
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

    public async Task<MemoryResponse> StoreAsync(StoreMemoryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = MemoryEntry.Create(
            request.Content,
            request.Type,
            new Namespace(request.Namespace),
            request.Tags,
            request.ExpiresAt);

        var embedding = await _embeddingProvider.GenerateEmbeddingAsync(request.Content, ct);
        entry.SetEmbedding(embedding);

        await _repository.AddAsync(entry, ct);
        await _vectorStore.UpsertAsync(entry.Id, embedding, entry.Namespace.Value, ct);

        _logger.LogInformation(
            "{IsAudit} Memory {MemoryId} created in namespace {Namespace}",
            true,
            entry.Id,
            entry.Namespace.Value);

        return entry.ToResponse();
    }

    public async Task<MemoryResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(id, ct)
            ?? throw new MemoryNotFoundException(id);

        entry.MarkAccessed();
        await _repository.UpdateAsync(entry, ct);

        return entry.ToResponse();
    }

    public async Task<MemoryResponse> UpdateAsync(UpdateMemoryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = await _repository.GetByIdAsync(request.Id, ct)
            ?? throw new MemoryNotFoundException(request.Id);

        if (!string.IsNullOrWhiteSpace(request.Content) &&
            !string.Equals(entry.Content, request.Content, StringComparison.Ordinal))
        {
            entry.UpdateContent(request.Content);
            var embedding = await _embeddingProvider.GenerateEmbeddingAsync(request.Content, ct);
            entry.SetEmbedding(embedding);
            await _vectorStore.UpsertAsync(entry.Id, embedding, entry.Namespace.Value, ct);
        }

        if (request.Type.HasValue)
        {
            entry.UpdateType(request.Type.Value);
        }

        if (request.Tags is not null)
        {
            entry.ReplaceTags(request.Tags);
        }

        if (request.ExpiresAt != entry.ExpiresAt)
        {
            entry.SetExpiration(request.ExpiresAt);
        }

        await _repository.UpdateAsync(entry, ct);

        _logger.LogInformation(
            "{IsAudit} Memory {MemoryId} updated in namespace {Namespace}",
            true,
            entry.Id,
            entry.Namespace.Value);

        return entry.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
        await _vectorStore.DeleteAsync(id, ct);

        _logger.LogInformation("{IsAudit} Memory {MemoryId} deleted", true, id);
    }

    public async Task<PaginatedList<MemoryResponse>> ListAsync(MemoryQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var normalizedFilter = filter with
        {
            Page = Math.Max(filter.Page, 1),
            PageSize = Math.Clamp(filter.PageSize, 1, 500)
        };

        var items = await _repository.ListAsync(normalizedFilter, ct);
        var totalCount = await _repository.CountAsync(normalizedFilter, ct);

        return new PaginatedList<MemoryResponse>
        {
            Items = items.Select(entry => entry.ToResponse()).ToArray(),
            TotalCount = totalCount,
            Page = normalizedFilter.Page,
            PageSize = normalizedFilter.PageSize
        };
    }

    public async Task WipeNamespaceAsync(string ns, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);

        await _repository.DeleteByNamespaceAsync(ns, ct);
        await _vectorStore.DeleteByNamespaceAsync(ns, ct);

        _logger.LogInformation("{IsAudit} Namespace {Namespace} wiped", true, ns.Trim().ToLowerInvariant());
    }

    public async Task LinkAsync(LinkMemoriesRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var source = await _repository.GetByIdAsync(request.SourceId, ct)
            ?? throw new MemoryNotFoundException(request.SourceId);
        _ = await _repository.GetByIdAsync(request.TargetId, ct)
            ?? throw new MemoryNotFoundException(request.TargetId);

        source.AddLink(MemoryLink.Create(
            request.SourceId,
            request.TargetId,
            request.Relation,
            request.Label));

        await _repository.UpdateAsync(source, ct);

        _logger.LogInformation(
            "{IsAudit} Memory {SourceId} linked to {TargetId} with relation {Relation}",
            true,
            request.SourceId,
            request.TargetId,
            request.Relation);
    }
}
