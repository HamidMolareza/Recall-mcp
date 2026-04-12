using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Server.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MemoryMcp.Server.Tests.Tools;

public sealed class ToolExecutionHelperTests
{
    [Fact]
    public async Task ExecuteAsync_WithModelNotFoundProviderError_ReturnsModelUnavailableError()
    {
        var response = (ToolResponse)await ToolExecutionHelper.ExecuteAsync(
            "test_tool",
            NullLogger.Instance,
            new TestOperationNoticeSink(),
            _ => Task.FromException<object>(new InvalidOperationException(
                "HTTP 503 (api_provider_error: model_not_found)\n\nno available channel for model text-embedding-3-small in group gapcode (request id: 20260412025139942968455eWD8McaU)")));

        response.Ok.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be("ai_model_unavailable");
        response.Error.Message.Should().Contain("model is not available");
        response.Error.Details.Should().Contain("model_not_found");
    }

    [Fact]
    public async Task ExecuteAsync_WithProviderFailureAndRequestIdDigits_DoesNotReturnQuotaError()
    {
        var response = (ToolResponse)await ToolExecutionHelper.ExecuteAsync(
            "test_tool",
            NullLogger.Instance,
            new TestOperationNoticeSink(),
            _ => Task.FromException<object>(new InvalidOperationException(
                "HTTP 503 (api_provider_error: upstream_failed) request id: 20260412025139942968455eWD8McaU")));

        response.Ok.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be("ai_provider_failed");
    }

    private sealed class TestOperationNoticeSink : IOperationNoticeSink
    {
        public void AddWarning(OperationWarning warning)
        {
        }

        public IReadOnlyList<OperationWarning> DrainWarnings() => [];
    }
}
