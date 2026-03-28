using Xunit;
using FluentAssertions;
using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Domain.Tests.ValueObjects;

public class EmbeddingVectorTests
{
    [Fact]
    public void Constructor_WithValidArray_SetsValues()
    {
        var vector = new EmbeddingVector([1.0f, 2.0f, 3.0f]);

        vector.Dimensions.Should().Be(3);
    }

    [Fact]
    public void Constructor_WithEmptyArray_ThrowsArgumentException()
    {
        var act = () => new EmbeddingVector([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var vector = new EmbeddingVector([1.0f, 0.0f, 0.0f]);

        vector.CosineSimilarity(vector).Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var v1 = new EmbeddingVector([1.0f, 0.0f]);
        var v2 = new EmbeddingVector([0.0f, 1.0f]);

        v1.CosineSimilarity(v2).Should().BeApproximately(0.0, 0.0001);
    }

    [Fact]
    public void CosineSimilarity_DifferentDimensions_ThrowsArgumentException()
    {
        var v1 = new EmbeddingVector([1.0f, 2.0f]);
        var v2 = new EmbeddingVector([1.0f, 2.0f, 3.0f]);

        var act = () => v1.CosineSimilarity(v2);

        act.Should().Throw<ArgumentException>();
    }
}
