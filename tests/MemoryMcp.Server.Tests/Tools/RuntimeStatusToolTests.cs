using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Persistence;
using MemoryMcp.Infrastructure.VectorSearch;
using MemoryMcp.Server.Runtime;
using MemoryMcp.Server.Tools;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MemoryMcp.Server.Tests.Tools;

public sealed class RuntimeStatusToolTests
{
    [Fact]
    public async Task GetRuntimeStatusAsync_ReturnsResolvedPathsAndPersistenceCounts()
    {
        var logDirectory = CreateTempDirectory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ConnectionString"] = "Data Source=app-data/memory.db",
                ["Logging:LogPath"] = logDirectory,
                ["Logging:GeneralLogFile"] = "app-.log",
                ["Logging:AuditLogFile"] = "audit-.log",
                ["Logging:ErrorLogFile"] = "errors-.log",
                ["Embedding:Provider"] = "GapGpt",
                ["Embedding:Model"] = "text-embedding-3-small",
                ["Embedding:BaseUrl"] = "https://api.gapgpt.app/v1",
                ["AI:Provider"] = "Simple",
                ["AI:Model"] = "gapgpt-qwen-3.5",
                ["AI:BaseUrl"] = "https://api.gapgpt.app/v1"
            })
            .Build();

        await File.WriteAllTextAsync(
            Path.Combine(logDirectory, "errors-20260412.log"),
            "2026-04-12 09:00:00.000 +03:30 [WRN] The GapGpt embedding request for model 'text-embedding-3-small' could not use the configured model because it is unavailable or unsupported. Local deterministic embeddings were used for this request." + Environment.NewLine);

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MemoryDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new MemoryDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var vectorStore = new InMemoryVectorStore(NullLogger<InMemoryVectorStore>.Instance);
        var memory = MemoryEntry.Create("Remember this", MemoryType.Fact, new Namespace("status/test"));
        memory.SetEmbedding(new EmbeddingVector([1.0f, 2.0f]));
        dbContext.Memories.Add(memory);
        await dbContext.SaveChangesAsync();
        await vectorStore.UpsertAsync(memory.Id, memory.Embedding!, memory.Namespace.Value);

        var tool = new RuntimeStatusTool(
            configuration,
            dbContext,
            vectorStore,
            new ProviderFallbackSummaryService(new FixedTimeProvider(new DateTimeOffset(2026, 4, 12, 12, 0, 0, TimeSpan.Zero))),
            new TestOperationNoticeSink(),
            NullLogger<RuntimeStatusTool>.Instance);

        var response = (ToolResponse)await tool.GetRuntimeStatusAsync();
        var data = response.Data.Should().BeOfType<RuntimeStatusData>().Subject;

        response.Ok.Should().BeTrue();
        Path.IsPathRooted(data.Storage.ResolvedDatabasePath!).Should().BeTrue();
        Path.IsPathRooted(data.Logging.ResolvedLogPath).Should().BeTrue();
        data.Persistence.MemoryCount.Should().Be(1);
        data.Persistence.EmbeddedMemoryCount.Should().Be(1);
        data.Providers.EmbeddingProvider.Should().Be("GapGpt");
        data.VectorIndex.EntryCount.Should().Be(1);
        data.VectorIndex.NamespacedEntryCount.Should().Be(1);
        data.VectorIndex.UnscopedEntryCount.Should().Be(0);
        data.VectorIndex.NamespaceCounts.Should().ContainKey("status/test").WhoseValue.Should().Be(1);
        data.ProviderFallbacks.TotalRecentEvents.Should().Be(1);
        data.ProviderFallbacks.Aggregates.Should().ContainSingle(aggregate => aggregate.Code == "ai_model_unavailable");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "runtime-status-tool-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private sealed class TestOperationNoticeSink : IOperationNoticeSink
    {
        public void AddWarning(OperationWarning warning)
        {
        }

        public IReadOnlyList<OperationWarning> DrainWarnings() => [];
    }
}
