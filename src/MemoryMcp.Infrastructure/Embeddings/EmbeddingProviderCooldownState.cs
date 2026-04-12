using MemoryMcp.Application.Diagnostics;

namespace MemoryMcp.Infrastructure.Embeddings;

public sealed class EmbeddingProviderCooldownState
{
    private readonly object _gate = new();
    private int _consecutiveConnectivityFailures;
    private DateTimeOffset? _cooldownUntil;
    private ExternalServiceFailureKind _cooldownReason = ExternalServiceFailureKind.Unknown;

    public EmbeddingProviderCooldownSnapshot GetSnapshot(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (_cooldownUntil is { } activeUntil && activeUntil > now)
            {
                return new EmbeddingProviderCooldownSnapshot(true, false, activeUntil, _cooldownReason);
            }

            if (_cooldownUntil is { } expiredUntil && expiredUntil <= now)
            {
                var expiredReason = _cooldownReason;
                Reset();
                return new EmbeddingProviderCooldownSnapshot(false, true, expiredUntil, expiredReason);
            }

            return new EmbeddingProviderCooldownSnapshot(false, false, null, ExternalServiceFailureKind.Unknown);
        }
    }

    public EmbeddingProviderCooldownTransition RecordFailure(
        ExternalServiceFailureKind failureKind,
        DateTimeOffset now,
        TimeSpan cooldownDuration,
        int cooldownFailureThreshold)
    {
        lock (_gate)
        {
            if (_cooldownUntil is { } activeUntil && activeUntil > now)
            {
                return new EmbeddingProviderCooldownTransition(false, activeUntil, _cooldownReason);
            }

            if (_cooldownUntil is { } expiredUntil && expiredUntil <= now)
            {
                Reset();
            }

            var shouldEnterCooldown = failureKind switch
            {
                ExternalServiceFailureKind.ModelUnavailable => true,
                ExternalServiceFailureKind.ConnectionFailed or ExternalServiceFailureKind.RequestTimedOut
                    => ++_consecutiveConnectivityFailures >= Math.Max(1, cooldownFailureThreshold),
                _ => false
            };

            if (!shouldEnterCooldown)
            {
                if (failureKind is not (ExternalServiceFailureKind.ConnectionFailed or ExternalServiceFailureKind.RequestTimedOut))
                {
                    _consecutiveConnectivityFailures = 0;
                }

                return new EmbeddingProviderCooldownTransition(false, null, failureKind);
            }

            _cooldownUntil = now.Add(cooldownDuration);
            _cooldownReason = failureKind;
            _consecutiveConnectivityFailures = 0;
            return new EmbeddingProviderCooldownTransition(true, _cooldownUntil, _cooldownReason);
        }
    }

    public void RecordSuccess()
    {
        lock (_gate)
        {
            Reset();
        }
    }

    private void Reset()
    {
        _consecutiveConnectivityFailures = 0;
        _cooldownUntil = null;
        _cooldownReason = ExternalServiceFailureKind.Unknown;
    }
}

public readonly record struct EmbeddingProviderCooldownSnapshot(
    bool IsActive,
    bool JustExpired,
    DateTimeOffset? CooldownUntil,
    ExternalServiceFailureKind Reason);

public readonly record struct EmbeddingProviderCooldownTransition(
    bool EnteredCooldown,
    DateTimeOffset? CooldownUntil,
    ExternalServiceFailureKind Reason);
