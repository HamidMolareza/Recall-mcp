using MemoryMcp.Application.Diagnostics;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MemoryMcp.Server.Tools;

public static class ToolExecutionHelper
{
    public static async Task<object> ExecuteAsync(
        string toolName,
        ILogger logger,
        IOperationNoticeSink operationNoticeSink,
        Func<CancellationToken, Task<object>> action,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operationNoticeSink);
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            var data = await action(ct);
            return ToolResponse.Success(data, operationNoticeSink.DrainWarnings());
        }
        catch (Exception ex)
        {
            var error = MapError(ex);
            logger.Log(error.LogLevel, ex, "Tool {ToolName} failed with code {ErrorCode}: {Message}", toolName, error.Error.Code, error.Error.Message);
            return ToolResponse.Failure(error.Error, operationNoticeSink.DrainWarnings());
        }
    }

    private static ToolErrorDefinition MapError(Exception exception)
    {
        var actual = Unwrap(exception);
        var details = actual.Message;

        return actual switch
        {
            _ when actual.GetType().FullName == "FluentValidation.ValidationException" => new ToolErrorDefinition(
                LogLevel.Information,
                new ToolError(
                    "validation_failed",
                    "The request arguments did not pass validation.",
                    "Review the required fields and value ranges, then retry.",
                    details)),
            MemoryNotFoundException notFound => new ToolErrorDefinition(
                LogLevel.Information,
                new ToolError(
                    "memory_not_found",
                    notFound.Message,
                    "Check the memory id first with list/search results, then retry the request.",
                    details)),
            DuplicateMemoryException duplicate => new ToolErrorDefinition(
                LogLevel.Information,
                new ToolError(
                    "duplicate_memory",
                    duplicate.Message,
                    "Use the existing memory id, update that record, or retry with rejectDuplicates=false.",
                    details)),
            DbUpdateConcurrencyException => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "concurrency_conflict",
                    "The memory changed while this request was being processed.",
                    "Fetch the latest memory state and retry the operation.",
                    details)),
            JsonException => new ToolErrorDefinition(
                LogLevel.Information,
                new ToolError(
                    "invalid_json_payload",
                    "The provided JSON payload is not valid for this operation.",
                    "Use JSON produced by export_memories or correct enum, date, and property values before retrying.",
                    details)),
            ArgumentException => new ToolErrorDefinition(
                LogLevel.Information,
                new ToolError(
                    "invalid_request",
                    NormalizeArgumentMessage(details),
                    "Review the tool arguments and retry with valid values.",
                    details)),
            OperationCanceledException => new ToolErrorDefinition(
                LogLevel.Information,
                new ToolError(
                    "operation_canceled",
                    "The request was canceled before it completed.",
                    "Retry the request if you still need the result.",
                    details)),
            InvalidOperationException when details.Contains("api key is required", StringComparison.OrdinalIgnoreCase) => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "provider_configuration_error",
                    "The configured AI provider is missing an API key.",
                    "Set the provider API key environment variable or appsettings value, then retry.",
                    details)),
            InvalidOperationException when details.Contains("returned no text", StringComparison.OrdinalIgnoreCase) => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "ai_empty_response",
                    "The AI provider returned an empty response.",
                    "Retry the request or switch to another model/provider if the problem continues.",
                    details)),
            _ => MapExternalFailure(actual)
        };
    }

    private static Exception Unwrap(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            var flattened = aggregate.Flatten();
            if (flattened.InnerExceptions.Count == 1)
            {
                return Unwrap(flattened.InnerExceptions[0]);
            }
        }

        return exception.InnerException is null ? exception : Unwrap(exception.InnerException);
    }

    private static string NormalizeArgumentMessage(string message)
    {
        if (message.StartsWith("Value cannot be null.", StringComparison.Ordinal))
        {
            return "A required input value was missing.";
        }

        var marker = " (Parameter '";
        var index = message.IndexOf(marker, StringComparison.Ordinal);
        return index > 0 ? message[..index] : message;
    }

    private static ToolErrorDefinition MapExternalFailure(Exception exception)
    {
        var analysis = ExternalServiceFailureAnalysis.Analyze(exception);

        return analysis.Kind switch
        {
            ExternalServiceFailureKind.ModelUnavailable => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "ai_model_unavailable",
                    "The configured AI or embedding model is not available from the provider.",
                    "Verify the model name and account access, or switch to a supported model before retrying.",
                    analysis.Details)),
            ExternalServiceFailureKind.QuotaOrRateLimit => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "ai_quota_or_rate_limit",
                    "The AI or embedding provider rejected the request because a quota, token, or rate limit was reached.",
                    "Check API quota, billing, or model limits, then retry after the limit window resets.",
                    analysis.Details)),
            ExternalServiceFailureKind.AuthenticationFailed => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "ai_authentication_failed",
                    "The AI or embedding provider rejected the request credentials.",
                    "Verify the API key, account permissions, and configured base URL before retrying.",
                    analysis.Details)),
            ExternalServiceFailureKind.RequestTimedOut => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "provider_timeout",
                    "The external provider did not respond before the timeout expired.",
                    "Retry the request or use another provider/model if timeouts continue.",
                    analysis.Details)),
            ExternalServiceFailureKind.ConnectionFailed => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "provider_connectivity_failed",
                    "The server could not reach the configured AI or embedding provider.",
                    "Check network access, DNS, firewall rules, and the configured provider endpoint before retrying.",
                    analysis.Details)),
            ExternalServiceFailureKind.ProviderFailure => new ToolErrorDefinition(
                LogLevel.Warning,
                new ToolError(
                    "ai_provider_failed",
                    "The AI or embedding provider returned an error for this request.",
                    "Inspect the provider response, then retry or switch providers or models if the issue continues.",
                    analysis.Details)),
            _ => new ToolErrorDefinition(
                LogLevel.Error,
                new ToolError(
                    "internal_error",
                    "The server could not complete this request.",
                    "Check the server logs for details, correct the underlying issue, and retry.",
                    exception.Message))
        };
    }

    private sealed record ToolErrorDefinition(LogLevel LogLevel, ToolError Error);
}
