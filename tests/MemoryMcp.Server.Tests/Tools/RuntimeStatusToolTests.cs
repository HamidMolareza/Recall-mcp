using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Persistence;
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
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ConnectionString"] = "Data Source=app-data/memory.db",
                ["Logging:LogPath"] = "app-data/logs",
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

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MemoryDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new MemoryDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var memory = MemoryEntry.Create("Remember this", MemoryType.Fact, new Namespace("status/test"));
        memory.SetEmbedding(new EmbeddingVector([1.0f, 2.0f]));
        dbContext.Memories.Add(memory);
        await dbContext.SaveChangesAsync();

        var tool = new RuntimeStatusTool(
            configuration,
            dbContext,
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
    }

    private sealed class TestOperationNoticeSink : IOperationNoticeSink
    {
        public void AddWarning(OperationWarning warning)
        {
        }

        public IReadOnlyList<OperationWarning> DrainWarnings() => [];
    }
}
