using Xunit;
using FluentAssertions;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.Tests.Fixtures;
using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Domain.Tests.Entities;

public class MemoryEntryTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsEntryWithCorrectState()
    {
        var entry = MemoryEntry.Create(
            "Test content",
            MemoryType.Fact,
            new Namespace("test-ns"),
            ["tag1", "tag2"]);

        entry.Id.Should().NotBeEmpty();
        entry.Content.Should().Be("Test content");
        entry.Type.Should().Be(MemoryType.Fact);
        entry.Namespace.Value.Should().Be("test-ns");
        entry.Tags.Should().HaveCount(2);
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithEmptyContent_ThrowsArgumentException()
    {
        var act = () => MemoryEntry.Create(
            "",
            MemoryType.Fact,
            new Namespace("test"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateContent_WithNewText_CreatesVersionEntry()
    {
        var entry = MemoryEntryFaker.CreateValid(content: "Original");

        entry.UpdateContent("Updated");

        entry.Content.Should().Be("Updated");
        entry.Versions.Should().HaveCount(1);
        entry.Versions[0].Content.Should().Be("Original");
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        var entry = MemoryEntryFaker.CreateValid(
            expiresAt: DateTime.UtcNow.AddDays(-1));

        entry.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenNoExpiration_ReturnsFalse()
    {
        var entry = MemoryEntryFaker.CreateValid();

        entry.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void AddTag_WithDuplicate_DoesNotAddTwice()
    {
        var entry = MemoryEntryFaker.CreateValid(tags: []);

        entry.AddTag("important");
        entry.AddTag("Important");

        entry.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void MarkAccessed_UpdatesLastAccessedAt()
    {
        var entry = MemoryEntryFaker.CreateValid();
        var before = entry.LastAccessedAt;

        Thread.Sleep(10);
        entry.MarkAccessed();

        entry.LastAccessedAt.Should().BeAfter(before);
    }
}
