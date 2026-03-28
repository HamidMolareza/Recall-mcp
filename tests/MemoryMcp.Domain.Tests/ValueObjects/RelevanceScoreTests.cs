using Xunit;
using FluentAssertions;
using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Domain.Tests.ValueObjects;

public class RelevanceScoreTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Constructor_WithValidValue_SetsValue(double value)
    {
        var score = new RelevanceScore(value);

        score.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Constructor_WithOutOfRangeValue_ThrowsArgumentOutOfRangeException(double value)
    {
        var act = () => new RelevanceScore(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
