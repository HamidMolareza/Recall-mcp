using MemoryMcp.Application.Interfaces;

namespace MemoryMcp.Infrastructure.Diagnostics;

public static class ExternalServiceFailureClassifier
{
    public static OperationWarning CreateFallbackWarning(
        Exception exception,
        string operation,
        string provider,
        string model,
        string fallbackAction)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackAction);

        var flattenedMessage = FlattenMessages(exception);
        var normalized = flattenedMessage.ToLowerInvariant();

        if (ContainsAny(normalized, "429", "quota", "rate limit", "too many requests", "insufficient_quota", "token limit", "tokens per"))
        {
            return new OperationWarning(
                "ai_quota_or_rate_limit",
                $"The {provider} {operation} for model '{model}' hit a quota, token, or rate limit. {fallbackAction}",
                "Check API quota, billing, model limits, or wait for the rate-limit window before retrying.",
                flattenedMessage);
        }

        if (ContainsAny(normalized, "401", "403", "unauthorized", "forbidden", "invalid api key", "authentication"))
        {
            return new OperationWarning(
                "ai_authentication_failed",
                $"The {provider} {operation} for model '{model}' was rejected by the provider. {fallbackAction}",
                "Verify the API key, provider permissions, and configured base URL before retrying.",
                flattenedMessage);
        }

        if (ContainsAny(normalized, "timeout", "timed out", "deadline exceeded"))
        {
            return new OperationWarning(
                "ai_request_timed_out",
                $"The {provider} {operation} for model '{model}' timed out. {fallbackAction}",
                "Retry the request or choose a faster model/provider if this keeps happening.",
                flattenedMessage);
        }

        if (ContainsAny(normalized, "permission denied", "connection refused", "host not found", "no such host", "name or service not known", "socketexception"))
        {
            return new OperationWarning(
                "ai_connection_failed",
                $"The {provider} {operation} for model '{model}' could not reach the provider endpoint. {fallbackAction}",
                "Check network access, DNS, firewall rules, and the configured provider base URL.",
                flattenedMessage);
        }

        return new OperationWarning(
            "ai_provider_failed",
            $"The {provider} {operation} for model '{model}' failed. {fallbackAction}",
            "Inspect the provider response and server logs, then retry or switch providers if needed.",
            flattenedMessage);
    }

    private static string FlattenMessages(Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current is not null)
        {
            messages.Add(current.Message);
            current = current.InnerException;
        }

        return string.Join(" | ", messages.Where(message => !string.IsNullOrWhiteSpace(message)));
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.Ordinal));
    }
}
