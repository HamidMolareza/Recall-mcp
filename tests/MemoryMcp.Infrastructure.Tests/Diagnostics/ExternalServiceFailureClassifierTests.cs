using FluentAssertions;
using MemoryMcp.Infrastructure.Diagnostics;
using Xunit;

namespace MemoryMcp.Infrastructure.Tests.Diagnostics;

public sealed class ExternalServiceFailureClassifierTests
{
    [Fact]
    public void CreateFallbackWarning_WithQuotaMessage_ReturnsQuotaWarning()
    {
        var exception = new InvalidOperationException("429 insufficient_quota: token limit exceeded");

        var warning = ExternalServiceFailureClassifier.CreateFallbackWarning(
            exception,
            "embedding request",
            "GapGpt",
            "text-embedding-3-small",
            "Local deterministic embeddings were used.");

        warning.Code.Should().Be("ai_quota_or_rate_limit");
        warning.Message.Should().Contain("quota");
        warning.SuggestedAction.Should().Contain("Check API quota");
    }

    [Fact]
    public void CreateFallbackWarning_WithConnectivityMessage_ReturnsConnectivityWarning()
    {
        var exception = new HttpRequestException("Permission denied (api.gapgpt.app:443)");

        var warning = ExternalServiceFailureClassifier.CreateFallbackWarning(
            exception,
            "summarization request",
            "GapGpt",
            "gapgpt-qwen-3.5",
            "The simple local summarizer was used.");

        warning.Code.Should().Be("ai_connection_failed");
        warning.Message.Should().Contain("could not reach");
        warning.SuggestedAction.Should().Contain("Check network access");
    }
}
