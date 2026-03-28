using FluentAssertions;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Persistence.Repositories;
using MemoryMcp.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MemoryMcp.Infrastructure.Tests.Persistence;

public sealed class MemoryRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task UpdateAsync_WithDetachedAggregate_ReplacesTagsAndPersistsVersion()
    {
        var entry = MemoryEntry.Create(
            "Original content",
            MemoryType.Fact,
            new Namespace("repo-test"),
            ["alpha", "beta"]);

        await using (var setupContext = _factory.CreateContext())
        {
            setupContext.Memories.Add(entry);
            await setupContext.SaveChangesAsync();
        }

        MemoryEntry detachedEntry;
        await using (var readContext = _factory.CreateContext())
        {
            var repository = CreateRepository(readContext);
            detachedEntry = (await repository.GetByIdAsync(entry.Id))!;
        }

        detachedEntry.UpdateContent("Updated content");
        detachedEntry.ReplaceTags(["gamma", "delta"]);

        await using (var writeContext = _factory.CreateContext())
        {
            var repository = CreateRepository(writeContext);
            await repository.UpdateAsync(detachedEntry);
        }

        await using (var assertContext = _factory.CreateContext())
        {
            var repository = CreateRepository(assertContext);
            var persisted = await repository.GetByIdAsync(entry.Id);

            persisted.Should().NotBeNull();
            persisted!.Content.Should().Be("Updated content");
            persisted.Tags.Select(tag => tag.Label).Should().BeEquivalentTo(["gamma", "delta"]);
            persisted.Versions.Should().ContainSingle(version => version.Content == "Original content");
        }
    }

    [Fact]
    public async Task UpdateAsync_WithDetachedAggregate_PersistsNewOutgoingLink()
    {
        var source = MemoryEntry.Create(
            "Source",
            MemoryType.Fact,
            new Namespace("repo-test"),
            ["alpha"]);
        var target = MemoryEntry.Create(
            "Target",
            MemoryType.Context,
            new Namespace("repo-test"),
            ["beta"]);

        await using (var setupContext = _factory.CreateContext())
        {
            setupContext.Memories.AddRange(source, target);
            await setupContext.SaveChangesAsync();
        }

        MemoryEntry detachedSource;
        await using (var readContext = _factory.CreateContext())
        {
            var repository = CreateRepository(readContext);
            detachedSource = (await repository.GetByIdAsync(source.Id))!;
        }

        detachedSource.AddLink(MemoryLink.Create(source.Id, target.Id, LinkRelation.References, "references"));

        await using (var writeContext = _factory.CreateContext())
        {
            var repository = CreateRepository(writeContext);
            await repository.UpdateAsync(detachedSource);
        }

        await using (var assertContext = _factory.CreateContext())
        {
            var repository = CreateRepository(assertContext);
            var persisted = await repository.GetByIdAsync(source.Id);

            persisted.Should().NotBeNull();
            persisted!.OutgoingLinks.Should().ContainSingle();
            persisted.OutgoingLinks[0].TargetId.Should().Be(target.Id);
            persisted.OutgoingLinks[0].Relation.Should().Be(LinkRelation.References);
            persisted.OutgoingLinks[0].Label.Should().Be("references");
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static MemoryRepository CreateRepository(MemoryMcp.Infrastructure.Persistence.MemoryDbContext context)
    {
        return new MemoryRepository(context, NullLogger<MemoryRepository>.Instance);
    }
}
