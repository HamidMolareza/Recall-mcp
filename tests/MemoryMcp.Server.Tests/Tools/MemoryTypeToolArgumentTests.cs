using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Server.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MemoryMcp.Server.Tests.Tools;

public sealed class MemoryTypeToolArgumentTests
{
    [Fact]
    public async Task StoreMemoryAsync_InvalidType_ReturnsStructuredError()
    {
        var tool = new StoreMemoryTool(
            null!,
            null!,
            null!,
            new TestOperationNoticeSink(),
            NullLogger<StoreMemoryTool>.Instance);

        var response = (ToolResponse)await tool.StoreMemoryAsync("Remember this", "project", "project/context");

        response.Ok.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be("invalid_request");
        response.Error.Message.Should().Contain("'type' must be one of:");
        response.Error.Details.Should().Contain("Fact");
    }

    [Fact]
    public async Task ListMemoriesAsync_InvalidType_ReturnsStructuredError()
    {
        var tool = new ListMemoriesTool(
            null!,
            null!,
            new TestOperationNoticeSink(),
            NullLogger<ListMemoriesTool>.Instance);

        var response = (ToolResponse)await tool.ListMemoriesAsync(type: "project");

        response.Ok.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be("invalid_request");
        response.Error.Message.Should().Contain("'type' must be one of:");
        response.Error.Details.Should().Contain("Entity");
    }

    [Fact]
    public async Task UpdateMemoryAsync_InvalidType_ReturnsStructuredError()
    {
        var tool = new UpdateMemoryTool(
            null!,
            null!,
            null!,
            new TestOperationNoticeSink(),
            NullLogger<UpdateMemoryTool>.Instance);

        var response = (ToolResponse)await tool.UpdateMemoryAsync(Guid.NewGuid(), type: "project");

        response.Ok.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be("invalid_request");
        response.Error.Message.Should().Contain("'type' must be one of:");
    }

    private sealed class TestOperationNoticeSink : IOperationNoticeSink
    {
        public void AddWarning(OperationWarning warning)
        {
        }

        public IReadOnlyList<OperationWarning> DrainWarnings() => [];
    }
}
