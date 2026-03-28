using Xunit;
using FluentAssertions;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Tests.Fixtures;

namespace MemoryMcp.Infrastructure.Tests.Persistence;

public class MemoryDbContextTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public MemoryDbContextTests()
    {
        _factory = new TestDbContextFactory();
    }

    [Fact]
    public async Task AddAndRetrieve_MemoryEntry_RoundTripsCorrectly()
    {
        var entry = MemoryEntry.Create(
            "Test content",
            MemoryType.Fact,
            new Namespace("test"),
            ["tag1"]);

        await using (var context = _factory.CreateContext())
        {
            context.Memories.Add(entry);
            await context.SaveChangesAsync();
        }

        await using (var context = _factory.CreateContext())
        {
            var loaded = await context.Memories.FindAsync(entry.Id);
            loaded.Should().NotBeNull();
            loaded!.Content.Should().Be("Test content");
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
