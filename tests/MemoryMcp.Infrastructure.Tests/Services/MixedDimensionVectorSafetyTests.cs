using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.VectorSearch;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace MemoryMcp.Infrastructure.Tests.Services;

public sealed class MixedDimensionVectorSafetyTests
{
    [Fact]
    public async Task FindDuplicatesAsync_SkipsCandidatesWithMismatchedDimensions()
    {
        var repository = Substitute.For<IMemoryRepository>();
        var embeddingProvider = Substitute.For<IEmbeddingProvider>();
        var vectorStore = new InMemoryVectorStore(NullLogger<InMemoryVectorStore>.Instance);
        var matchingId = Guid.NewGuid();
        var mismatchedId = Guid.NewGuid();
        var queryVector = new EmbeddingVector([1.0f, 0.0f, 0.0f]);

        await vectorStore.UpsertAsync(matchingId, new EmbeddingVector([1.0f, 0.0f, 0.0f]), "project/context");
        await vectorStore.UpsertAsync(mismatchedId, new EmbeddingVector([1.0f, 0.0f]), "project/context");

        repository.GetByIdAsync(matchingId, Arg.Any<CancellationToken>())
            .Returns(CreateMemoryEntryWithId(matchingId, "Matching duplicate", "project/context"));

        var sut = new DeduplicationService(
            repository,
            embeddingProvider,
            vectorStore,
            NullLogger<DeduplicationService>.Instance);

        var duplicates = await sut.FindDuplicatesAsync(
            "Matching duplicate",
            embedding: queryVector,
            threshold: 0.95);

        duplicates.Should().Equal(matchingId);
        await repository.DidNotReceive().GetByIdAsync(mismatchedId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectContradictionsAsync_SkipsCandidatesWithMismatchedDimensions()
    {
        var repository = Substitute.For<IMemoryRepository>();
        var embeddingProvider = Substitute.For<IEmbeddingProvider>();
        var vectorStore = new InMemoryVectorStore(NullLogger<InMemoryVectorStore>.Instance);
        var matchingId = Guid.NewGuid();
        var mismatchedId = Guid.NewGuid();
        var queryVector = new EmbeddingVector([1.0f, 0.0f, 0.0f]);

        await vectorStore.UpsertAsync(matchingId, new EmbeddingVector([1.0f, 0.0f, 0.0f]), "project/context");
        await vectorStore.UpsertAsync(mismatchedId, new EmbeddingVector([1.0f, 0.0f]), "project/context");

        repository.GetByIdAsync(matchingId, Arg.Any<CancellationToken>())
            .Returns(CreateMemoryEntryWithId(matchingId, "Feature is enabled", "project/context"));

        var sut = new ContradictionDetector(
            repository,
            embeddingProvider,
            vectorStore,
            NullLogger<ContradictionDetector>.Instance);

        var contradictions = await sut.DetectContradictionsAsync(
            "Feature is not enabled",
            "project/context",
            embedding: queryVector);

        contradictions.Should().Equal(matchingId);
        await repository.DidNotReceive().GetByIdAsync(mismatchedId, Arg.Any<CancellationToken>());
    }

    private static MemoryEntry CreateMemoryEntryWithId(Guid id, string content, string ns)
    {
        var entry = MemoryEntry.Create(content, MemoryType.Fact, new Namespace(ns));
        typeof(MemoryEntry).GetProperty(nameof(MemoryEntry.Id))!.SetValue(entry, id);
        return entry;
    }
}
