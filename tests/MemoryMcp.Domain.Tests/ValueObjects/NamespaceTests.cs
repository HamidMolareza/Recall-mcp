using Xunit;
using FluentAssertions;
using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Domain.Tests.ValueObjects;

public class NamespaceTests
{
    [Fact]
    public void Constructor_WithValidValue_NormalizesToLowerCase()
    {
        var ns = new Namespace("Work/Project-X");

        ns.Value.Should().Be("work/project-x");
    }

    [Fact]
    public void Constructor_WithEmptyValue_ThrowsArgumentException()
    {
        var act = () => new Namespace("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsChildOf_WhenNestedUnderParent_ReturnsTrue()
    {
        var parent = new Namespace("work");
        var child = new Namespace("work/project-x");

        child.IsChildOf(parent).Should().BeTrue();
    }

    [Fact]
    public void IsChildOf_WhenNotNested_ReturnsFalse()
    {
        var ns1 = new Namespace("work");
        var ns2 = new Namespace("personal");

        ns2.IsChildOf(ns1).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var ns1 = new Namespace("work");
        var ns2 = new Namespace("Work");

        ns1.Should().Be(ns2);
    }
}
