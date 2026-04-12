using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Embeddings;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace MemoryMcp.Infrastructure.Tests.Embeddings;

public sealed class OpenAiCompatibleEmbeddingProviderTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_AfterModelUnavailableFailure_UsesCooldownAndSkipsNextRemoteAttempt()
    {
        var remoteGenerator = Substitute.For<IRemoteEmbeddingGenerator>();
        remoteGenerator.GenerateAsync("first", Arg.Any<CancellationToken>())
            .Returns<Task<EmbeddingVector>>(_ => throw new InvalidOperationException("HTTP 503 (api_provider_error: model_not_found)"));

        var sink = new OperationNoticeSink();
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 4, 12, 6, 0, 0, TimeSpan.Zero));
        var provider = CreateProvider(remoteGenerator, sink, timeProvider);

        var firstResult = await provider.GenerateEmbeddingAsync("first");
        var firstWarnings = sink.DrainWarnings();

        var secondResult = await provider.GenerateEmbeddingAsync("second");
        var secondWarnings = sink.DrainWarnings();

        firstResult.Values.Should().NotBeEmpty();
        firstWarnings.Should().ContainSingle(warning => warning.Code == "ai_model_unavailable");
        secondResult.Values.Should().NotBeEmpty();
        secondWarnings.Should().ContainSingle(warning => warning.Code == "embedding_provider_cooldown_active");
        await remoteGenerator.Received(1).GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_RepeatedConnectionFailures_EnterCooldownAfterThreshold()
    {
        var remoteGenerator = Substitute.For<IRemoteEmbeddingGenerator>();
        remoteGenerator.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<EmbeddingVector>>(_ => throw new HttpRequestException("Connection refused"));

        var sink = new OperationNoticeSink();
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 4, 12, 6, 0, 0, TimeSpan.Zero));
        var provider = CreateProvider(
            remoteGenerator,
            sink,
            timeProvider,
            new EmbeddingProviderOptions
            {
                Provider = "GapGpt",
                Model = "text-embedding-3-small",
                BaseUrl = "https://api.gapgpt.app/v1",
                ApiKey = "test-key",
                Dimensions = 32,
                CooldownSeconds = 120,
                CooldownFailureThreshold = 3
            });

        await provider.GenerateEmbeddingAsync("one");
        sink.DrainWarnings().Should().ContainSingle(warning => warning.Code == "ai_connection_failed");

        await provider.GenerateEmbeddingAsync("two");
        sink.DrainWarnings().Should().ContainSingle(warning => warning.Code == "ai_connection_failed");

        await provider.GenerateEmbeddingAsync("three");
        sink.DrainWarnings().Should().ContainSingle(warning => warning.Code == "ai_connection_failed");

        await provider.GenerateEmbeddingAsync("four");
        sink.DrainWarnings().Should().ContainSingle(warning => warning.Code == "embedding_provider_cooldown_active");
        await remoteGenerator.Received(3).GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_AfterCooldownExpires_RetriesRemoteProvider()
    {
        var remoteGenerator = Substitute.For<IRemoteEmbeddingGenerator>();
        remoteGenerator.GenerateAsync("first", Arg.Any<CancellationToken>())
            .Returns<Task<EmbeddingVector>>(_ => throw new InvalidOperationException("HTTP 503 (api_provider_error: model_not_found)"));
        remoteGenerator.GenerateAsync("second", Arg.Any<CancellationToken>())
            .Returns(new EmbeddingVector([9f, 8f, 7f]));

        var sink = new OperationNoticeSink();
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 4, 12, 6, 0, 0, TimeSpan.Zero));
        var provider = CreateProvider(
            remoteGenerator,
            sink,
            timeProvider,
            new EmbeddingProviderOptions
            {
                Provider = "GapGpt",
                Model = "text-embedding-3-small",
                BaseUrl = "https://api.gapgpt.app/v1",
                ApiKey = "test-key",
                Dimensions = 32,
                CooldownSeconds = 30,
                CooldownFailureThreshold = 3
            });

        await provider.GenerateEmbeddingAsync("first");
        sink.DrainWarnings().Should().ContainSingle(warning => warning.Code == "ai_model_unavailable");

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var result = await provider.GenerateEmbeddingAsync("second");
        var warnings = sink.DrainWarnings();

        result.Values.Should().Equal(9f, 8f, 7f);
        warnings.Should().BeEmpty();
        await remoteGenerator.Received(2).GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static OpenAiCompatibleEmbeddingProvider CreateProvider(
        IRemoteEmbeddingGenerator remoteGenerator,
        IOperationNoticeSink sink,
        TimeProvider timeProvider,
        EmbeddingProviderOptions? options = null)
    {
        return new OpenAiCompatibleEmbeddingProvider(
            remoteGenerator,
            options ?? new EmbeddingProviderOptions
            {
                Provider = "GapGpt",
                Model = "text-embedding-3-small",
                BaseUrl = "https://api.gapgpt.app/v1",
                ApiKey = "test-key",
                Dimensions = 32,
                CooldownSeconds = 120,
                CooldownFailureThreshold = 3
            },
            sink,
            new EmbeddingProviderCooldownState(),
            timeProvider,
            NullLogger<OpenAiCompatibleEmbeddingProvider>.Instance);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan by)
        {
            _utcNow = _utcNow.Add(by);
        }
    }
}
