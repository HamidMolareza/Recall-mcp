using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Persistence;
using MemoryMcp.Infrastructure.VectorSearch;
using MemoryMcp.Server.Tools;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MemoryMcp.Server.Tests.Tools;

public sealed class RebuildVectorIndexToolTests
{
    [Fact]
    public async Task RebuildVectorIndexAsync_ReplacesStaleEntriesWithPersistedEmbeddings()
    {
        var options = new DbContextOptionsBuilder<MemoryDbContext>()
            .UseSqlite(CreateOpenConnection())
            .Options;

        await using var dbContext = new MemoryDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var persisted = MemoryEntry.Create("Persisted", MemoryType.Fact, new Namespace("status/rebuild"));
        persisted.SetEmbedding(new EmbeddingVector([1.0f, 2.0f]));
        dbContext.Memories.Add(persisted);
        await dbContext.SaveChangesAsync();

        var vectorStore = new InMemoryVectorStore(NullLogger<InMemoryVectorStore>.Instance);
        await vectorStore.UpsertAsync(Guid.NewGuid(), new EmbeddingVector([9.0f, 9.0f]), "stale/entry");
        var embeddingProvider = new StubEmbeddingProvider(_ => new EmbeddingVector([1.0f, 2.0f]));

        var tool = new RebuildVectorIndexTool(
            new VectorIndexMaintenanceService(
                dbContext,
                vectorStore,
                embeddingProvider,
                NullLogger<VectorIndexMaintenanceService>.Instance),
            new TestOperationNoticeSink(),
            NullLogger<RebuildVectorIndexTool>.Instance);

        var response = (ToolResponse)await tool.RebuildVectorIndexAsync();
        var data = response.Data.Should().BeOfType<RebuildVectorIndexResult>().Subject;

        response.Ok.Should().BeTrue();
        data.PersistedEmbeddingCount.Should().Be(1);
        data.ReembeddedMemoryCount.Should().Be(0);
        data.VectorIndex.EntryCount.Should().Be(1);
        data.VectorIndex.NamespaceCounts.Should().ContainKey("status/rebuild").WhoseValue.Should().Be(1);

        var searchResults = await vectorStore.SearchAsync(new EmbeddingVector([1.0f, 2.0f]), 10);
        searchResults.Should().ContainSingle(result => result.MemoryId == persisted.Id);
    }

    [Fact]
    public async Task RebuildVectorIndexAsync_WithRegenerateEmbeddings_ReembedsPersistedMemories()
    {
        var options = new DbContextOptionsBuilder<MemoryDbContext>()
            .UseSqlite(CreateOpenConnection())
            .Options;

        await using var dbContext = new MemoryDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var persisted = MemoryEntry.Create("Persisted", MemoryType.Fact, new Namespace("status/rebuild"));
        persisted.SetEmbedding(new EmbeddingVector([9.0f, 9.0f]));
        dbContext.Memories.Add(persisted);
        await dbContext.SaveChangesAsync();

        var vectorStore = new InMemoryVectorStore(NullLogger<InMemoryVectorStore>.Instance);
        var embeddingProvider = new StubEmbeddingProvider(content =>
            content == "Persisted"
                ? new EmbeddingVector([3.0f, 2.0f, 1.0f])
                : throw new InvalidOperationException("Unexpected content."));

        var tool = new RebuildVectorIndexTool(
            new VectorIndexMaintenanceService(
                dbContext,
                vectorStore,
                embeddingProvider,
                NullLogger<VectorIndexMaintenanceService>.Instance),
            new TestOperationNoticeSink(),
            NullLogger<RebuildVectorIndexTool>.Instance);

        var response = (ToolResponse)await tool.RebuildVectorIndexAsync(regenerateEmbeddings: true);
        var data = response.Data.Should().BeOfType<RebuildVectorIndexResult>().Subject;

        response.Ok.Should().BeTrue();
        data.PersistedEmbeddingCount.Should().Be(1);
        data.ReembeddedMemoryCount.Should().Be(1);

        var reloadedEntry = await dbContext.Memories.AsNoTracking().SingleAsync(memory => memory.Id == persisted.Id);
        reloadedEntry.Embedding.Should().NotBeNull();
        reloadedEntry.Embedding!.Values.Should().Equal([3.0f, 2.0f, 1.0f]);

        var searchResults = await vectorStore.SearchAsync(new EmbeddingVector([3.0f, 2.0f, 1.0f]), 10);
        searchResults.Should().ContainSingle(result => result.MemoryId == persisted.Id);
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private sealed class TestOperationNoticeSink : IOperationNoticeSink
    {
        public void AddWarning(OperationWarning warning)
        {
        }

        public IReadOnlyList<OperationWarning> DrainWarnings() => [];
    }

    private sealed class StubEmbeddingProvider(Func<string, EmbeddingVector> generator) : IEmbeddingProvider
    {
        public Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(generator(text));
        }
    }
}
