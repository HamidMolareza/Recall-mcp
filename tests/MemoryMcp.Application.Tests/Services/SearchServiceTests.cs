using Xunit;
using FluentAssertions;
using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MemoryMcp.Application.Tests.Services;

public class SearchServiceTests
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<SearchService> _logger;
    private readonly SearchService _sut;

    public SearchServiceTests()
    {
        _repository = Substitute.For<IMemoryRepository>();
        _embeddingProvider = Substitute.For<IEmbeddingProvider>();
        _vectorStore = Substitute.For<IVectorStore>();
        _logger = Substitute.For<ILogger<SearchService>>();

        _sut = new SearchService(_repository, _embeddingProvider, _vectorStore, _logger);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        Assert.NotNull(_sut);
    }

    [Fact]
    public async Task SemanticSearchAsync_FiltersExpiredEntries_AndReturnsRankedResults()
    {
        var queryVector = new EmbeddingVector([1.0f, 0.0f]);
        _embeddingProvider.GenerateEmbeddingAsync("query", Arg.Any<CancellationToken>())
            .Returns(queryVector);
        _vectorStore.SearchAsync(queryVector, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new VectorSearchResult(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 0.92),
                new VectorSearchResult(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 0.85)
            ]);

        var activeEntry = MemoryEntry.Create(
            "Active content",
            MemoryType.Fact,
            new Namespace("work"),
            ["tag1"]);
        typeof(MemoryEntry).GetProperty(nameof(MemoryEntry.Id))!.SetValue(
            activeEntry,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        var expiredEntry = MemoryEntry.Create(
            "Expired content",
            MemoryType.Fact,
            new Namespace("work"),
            ["tag1"],
            DateTime.UtcNow.AddDays(-1));
        typeof(MemoryEntry).GetProperty(nameof(MemoryEntry.Id))!.SetValue(
            expiredEntry,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        _repository.GetByIdAsync(activeEntry.Id, Arg.Any<CancellationToken>()).Returns(activeEntry);
        _repository.GetByIdAsync(expiredEntry.Id, Arg.Any<CancellationToken>()).Returns(expiredEntry);

        var result = await _sut.SemanticSearchAsync(new SearchMemoryRequest
        {
            Query = "query",
            Namespace = "work",
            Tags = ["tag1"],
            TopK = 5
        });

        result.Should().HaveCount(1);
        result[0].Memory.Id.Should().Be(activeEntry.Id);
        result[0].Score.Should().BeApproximately(0.92, 0.0001);
    }
}
