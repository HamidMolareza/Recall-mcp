using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using Xunit;

namespace MemoryMcp.Application.Tests.Services;

public sealed class OperationNoticeSinkTests
{
    [Fact]
    public void DrainWarnings_ReturnsUniqueWarnings_AndClearsTheSink()
    {
        IOperationNoticeSink sink = new OperationNoticeSink();
        var warning = new OperationWarning(
            "ai_quota_or_rate_limit",
            "The provider hit a quota limit.",
            "Check billing.");

        sink.AddWarning(warning);
        sink.AddWarning(warning);

        var firstDrain = sink.DrainWarnings();
        var secondDrain = sink.DrainWarnings();

        firstDrain.Should().ContainSingle().Which.Should().Be(warning);
        secondDrain.Should().BeEmpty();
    }
}
