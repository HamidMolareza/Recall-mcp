using Xunit;
using FluentAssertions;
using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.Exceptions;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MemoryMcp.Application.Tests.Services;

public class MemoryServiceTests
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<MemoryService> _logger;
    private readonly MemoryService _sut;

    public MemoryServiceTests()
    {
        _repository = Substitute.For<IMemoryRepository>();
        _embeddingProvider = Substitute.For<IEmbeddingProvider>();
        _vectorStore = Substitute.For<IVectorStore>();
        _logger = Substitute.For<ILogger<MemoryService>>();

        _sut = new MemoryService(_repository, _embeddingProvider, _vectorStore, _logger);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act covered by constructor in setup
        // Assert — instance was created
        Assert.NotNull(_sut);
    }

    [Fact]
    public async Task StoreAsync_WithValidRequest_PersistsMemoryAndUpsertsVector()
    {
        var vector = new EmbeddingVector([1.0f, 2.0f, 3.0f]);
        _embeddingProvider.GenerateEmbeddingAsync("Test content", Arg.Any<CancellationToken>())
            .Returns(vector);

        MemoryEntry? persistedEntry = null;
        _repository.When(repo => repo.AddAsync(Arg.Any<MemoryEntry>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => persistedEntry = callInfo.Arg<MemoryEntry>());

        var result = await _sut.StoreAsync(new StoreMemoryRequest
        {
            Content = "Test content",
            Type = MemoryType.Fact,
            Namespace = "work/project-x",
            Tags = ["tag1", "tag2"]
        });

        result.Content.Should().Be("Test content");
        result.Namespace.Should().Be("work/project-x");
        result.Tags.Should().BeEquivalentTo(["tag1", "tag2"]);
        persistedEntry.Should().NotBeNull();
        persistedEntry!.Embedding.Should().Be(vector);

        await _repository.Received(1).AddAsync(Arg.Any<MemoryEntry>(), Arg.Any<CancellationToken>());
        await _vectorStore.Received(1)
            .UpsertAsync(persistedEntry.Id, vector, "work/project-x", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreAsync_WithPrecomputedEmbedding_DoesNotCallEmbeddingProvider()
    {
        var vector = new EmbeddingVector([4.0f, 5.0f, 6.0f]);

        await _sut.StoreAsync(new StoreMemoryRequest
        {
            Content = "Test content",
            Type = MemoryType.Fact,
            Namespace = "work/project-x",
            Embedding = vector
        });

        await _embeddingProvider.DidNotReceiveWithAnyArgs().GenerateEmbeddingAsync(default!, default);
        await _vectorStore.Received(1)
            .UpsertAsync(Arg.Any<Guid>(), vector, "work/project-x", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithPrecomputedEmbedding_DoesNotCallEmbeddingProvider()
    {
        var existing = MemoryEntry.Create(
            "Old content",
            MemoryType.Fact,
            new Namespace("work/project-x"),
            ["tag1"]);
        var vector = new EmbeddingVector([7.0f, 8.0f, 9.0f]);

        _repository.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.UpdateAsync(new UpdateMemoryRequest
        {
            Id = existing.Id,
            Content = "New content",
            Embedding = vector
        });

        result.Content.Should().Be("New content");
        await _embeddingProvider.DidNotReceiveWithAnyArgs().GenerateEmbeddingAsync(default!, default);
        await _vectorStore.Received(1)
            .UpsertAsync(existing.Id, vector, "work/project-x", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenMemoryMissing_ThrowsMemoryNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((MemoryEntry?)null);

        var act = () => _sut.GetByIdAsync(id);

        await act.Should().ThrowAsync<MemoryNotFoundException>();
    }
}
