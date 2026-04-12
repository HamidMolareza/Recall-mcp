using FluentAssertions;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.VectorSearch;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MemoryMcp.Infrastructure.Tests.VectorSearch;

public class InMemoryVectorStoreTests
{
    [Fact]
    public async Task SearchAsync_ReturnsResultsOrderedBySimilarity()
    {
        var logger = Substitute.For<ILogger<InMemoryVectorStore>>();
        var store = new InMemoryVectorStore(logger);

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        await store.UpsertAsync(firstId, new EmbeddingVector([1.0f, 0.0f]), "work");
        await store.UpsertAsync(secondId, new EmbeddingVector([0.5f, 0.5f]), "work");

        var results = await store.SearchAsync(new EmbeddingVector([1.0f, 0.0f]), 10);

        results.Should().HaveCount(2);
        results[0].MemoryId.Should().Be(firstId);
        results[0].Score.Should().BeGreaterThan(results[1].Score);
    }

    [Fact]
    public async Task GetSnapshot_ReturnsEntryCountsByNamespace()
    {
        var logger = Substitute.For<ILogger<InMemoryVectorStore>>();
        var store = new InMemoryVectorStore(logger);

        await store.UpsertAsync(Guid.NewGuid(), new EmbeddingVector([1.0f, 0.0f]), "work/project-a");
        await store.UpsertAsync(Guid.NewGuid(), new EmbeddingVector([0.5f, 0.5f]), "work/project-a");
        await store.UpsertAsync(Guid.NewGuid(), new EmbeddingVector([0.0f, 1.0f]));

        var snapshot = store.GetSnapshot();

        snapshot.EntryCount.Should().Be(3);
        snapshot.NamespacedEntryCount.Should().Be(2);
        snapshot.UnscopedEntryCount.Should().Be(1);
        snapshot.NamespaceCounts.Should().ContainKey("work/project-a").WhoseValue.Should().Be(2);
    }
}
