using FluentAssertions;
using MemoryMcp.Server.Runtime;
using Xunit;

namespace MemoryMcp.Server.Tests.Runtime;

public sealed class ProviderFallbackSummaryServiceTests
{
    [Fact]
    public async Task SummarizeAsync_AggregatesRecentFallbackWarningsFromErrorLogs()
    {
        var logDirectory = Path.Combine(Path.GetTempPath(), "provider-fallback-summary-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(logDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(logDirectory, "errors-20260412.log"),
            string.Join(
                Environment.NewLine,
                "2026-04-12 09:00:00.000 +03:30 [WRN] The GapGpt embedding request for model 'text-embedding-3-small' could not use the configured model because it is unavailable or unsupported. Local deterministic embeddings were used for this request.",
                "2026-04-12 09:05:00.000 +03:30 [WRN] The GapGpt embedding provider for model 'text-embedding-3-small' is temporarily skipping remote requests until 2026-04-12T09:10:00.0000000+03:30 after recent configured model unavailability. Local deterministic embeddings were used for this request.",
                "2026-04-10 09:05:00.000 +03:30 [WRN] The GapGpt embedding request for model 'old-model' failed. Local deterministic embeddings were used for this request."));

        var service = new ProviderFallbackSummaryService(
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 12, 12, 0, 0, TimeSpan.Zero)));

        var summary = await service.SummarizeAsync(logDirectory, "errors-.log");

        summary.TotalRecentEvents.Should().Be(2);
        summary.Aggregates.Should().HaveCount(2);
        summary.Aggregates.Should().ContainSingle(item => item.Code == "ai_model_unavailable" && item.Count == 1);
        summary.Aggregates.Should().ContainSingle(item => item.Code == "embedding_provider_cooldown_active" && item.Count == 1);
        summary.LastOccurredAt.Should().NotBeNull();
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
