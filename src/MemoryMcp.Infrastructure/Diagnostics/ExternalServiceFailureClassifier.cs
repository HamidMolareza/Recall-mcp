using MemoryMcp.Application.Diagnostics;
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
        return CreateFallbackWarning(
            ExternalServiceFailureAnalysis.Analyze(exception),
            operation,
            provider,
            model,
            fallbackAction);
    }

    public static OperationWarning CreateFallbackWarning(
        ExternalServiceFailureInfo analysis,
        string operation,
        string provider,
        string model,
        string fallbackAction)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackAction);

        return analysis.Kind switch
        {
            ExternalServiceFailureKind.ModelUnavailable => new OperationWarning(
                "ai_model_unavailable",
                $"The {provider} {operation} for model '{model}' could not use the configured model because it is unavailable or unsupported. {fallbackAction}",
                "Verify the model name and account access for this provider, or switch to a supported model before retrying.",
                analysis.Details),
            ExternalServiceFailureKind.QuotaOrRateLimit => new OperationWarning(
                "ai_quota_or_rate_limit",
                $"The {provider} {operation} for model '{model}' hit a quota, token, or rate limit. {fallbackAction}",
                "Check API quota, billing, model limits, or wait for the rate-limit window before retrying.",
                analysis.Details),
            ExternalServiceFailureKind.AuthenticationFailed => new OperationWarning(
                "ai_authentication_failed",
                $"The {provider} {operation} for model '{model}' was rejected by the provider. {fallbackAction}",
                "Verify the API key, provider permissions, and configured base URL before retrying.",
                analysis.Details),
            ExternalServiceFailureKind.RequestTimedOut => new OperationWarning(
                "ai_request_timed_out",
                $"The {provider} {operation} for model '{model}' timed out. {fallbackAction}",
                "Retry the request or choose a faster model/provider if this keeps happening.",
                analysis.Details),
            ExternalServiceFailureKind.ConnectionFailed => new OperationWarning(
                "ai_connection_failed",
                $"The {provider} {operation} for model '{model}' could not reach the provider endpoint. {fallbackAction}",
                "Check network access, DNS, firewall rules, and the configured provider base URL.",
                analysis.Details),
            _ => new OperationWarning(
                "ai_provider_failed",
                $"The {provider} {operation} for model '{model}' failed. {fallbackAction}",
                "Inspect the provider response and server logs, then retry or switch providers if needed.",
                analysis.Details)
        };
    }
}
