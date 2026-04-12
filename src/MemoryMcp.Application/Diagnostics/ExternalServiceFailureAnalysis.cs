using System.Text.RegularExpressions;

namespace MemoryMcp.Application.Diagnostics;

public enum ExternalServiceFailureKind
{
    Unknown,
    ModelUnavailable,
    QuotaOrRateLimit,
    AuthenticationFailed,
    RequestTimedOut,
    ConnectionFailed,
    ProviderFailure
}

public sealed record ExternalServiceFailureInfo(
    ExternalServiceFailureKind Kind,
    string Details);

public static partial class ExternalServiceFailureAnalysis
{
    private static readonly string[] ModelUnavailableMarkers =
    [
        "model_not_found",
        "model not found",
        "unsupported model",
        "unknown model",
        "no available channel for model",
        "model is not available"
    ];

    private static readonly string[] QuotaMarkers =
    [
        "quota",
        "rate limit",
        "rate_limit",
        "too many requests",
        "insufficient_quota",
        "quota exceeded",
        "token limit",
        "tokens per"
    ];

    private static readonly string[] AuthenticationMarkers =
    [
        "unauthorized",
        "forbidden",
        "invalid api key",
        "invalid_api_key",
        "authentication"
    ];

    private static readonly string[] TimeoutMarkers =
    [
        "timeout",
        "timed out",
        "deadline exceeded"
    ];

    private static readonly string[] ConnectionMarkers =
    [
        "permission denied",
        "connection refused",
        "host not found",
        "no such host",
        "name or service not known",
        "socketexception",
        "dns"
    ];

    public static ExternalServiceFailureInfo Analyze(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var details = FlattenMessages(exception);
        var normalized = details.ToLowerInvariant();
        var statusCode = ExtractHttpStatusCode(details);

        if (ContainsAny(normalized, ModelUnavailableMarkers))
        {
            return new ExternalServiceFailureInfo(ExternalServiceFailureKind.ModelUnavailable, details);
        }

        if (statusCode == 429 || ContainsAny(normalized, QuotaMarkers))
        {
            return new ExternalServiceFailureInfo(ExternalServiceFailureKind.QuotaOrRateLimit, details);
        }

        if (statusCode is 401 or 403 || ContainsAny(normalized, AuthenticationMarkers))
        {
            return new ExternalServiceFailureInfo(ExternalServiceFailureKind.AuthenticationFailed, details);
        }

        if (statusCode == 408 || ContainsAny(normalized, TimeoutMarkers))
        {
            return new ExternalServiceFailureInfo(ExternalServiceFailureKind.RequestTimedOut, details);
        }

        if (ContainsAny(normalized, ConnectionMarkers))
        {
            return new ExternalServiceFailureInfo(ExternalServiceFailureKind.ConnectionFailed, details);
        }

        if (statusCode is >= 500 and <= 599 || normalized.Contains("api_provider_error", StringComparison.Ordinal))
        {
            return new ExternalServiceFailureInfo(ExternalServiceFailureKind.ProviderFailure, details);
        }

        return new ExternalServiceFailureInfo(ExternalServiceFailureKind.Unknown, details);
    }

    private static string FlattenMessages(Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current is not null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                messages.Add(current.Message);
            }

            current = current.InnerException;
        }

        return string.Join(" | ", messages);
    }

    private static bool ContainsAny(string value, IEnumerable<string> candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.Ordinal));
    }

    private static int? ExtractHttpStatusCode(string value)
    {
        var match = HttpStatusCodeRegex().Match(value);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups["code"].Value, out var code)
            ? code
            : null;
    }

    [GeneratedRegex(@"\b(?:http|status(?:\s+code)?)\s*[:=]?\s*(?<code>\d{3})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HttpStatusCodeRegex();
}
