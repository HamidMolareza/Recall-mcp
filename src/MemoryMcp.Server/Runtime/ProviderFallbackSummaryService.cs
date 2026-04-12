using System.Globalization;
using System.Text.RegularExpressions;

namespace MemoryMcp.Server.Runtime;

public sealed partial class ProviderFallbackSummaryService
{
    private static readonly TimeSpan DefaultLookbackWindow = TimeSpan.FromHours(24);

    private readonly TimeProvider _timeProvider;

    public ProviderFallbackSummaryService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<ProviderFallbackSummary> SummarizeAsync(
        string logPath,
        string errorLogFileTemplate,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorLogFileTemplate);
        ct.ThrowIfCancellationRequested();

        var directory = new DirectoryInfo(logPath);
        if (!directory.Exists)
        {
            return Task.FromResult(new ProviderFallbackSummary(DefaultLookbackWindow, 0, null, []));
        }

        var cutoff = _timeProvider.GetUtcNow() - DefaultLookbackWindow;
        var events = directory
            .EnumerateFiles(BuildSearchPattern(errorLogFileTemplate), SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(7)
            .SelectMany(file => ReadFallbackEvents(file.FullName, cutoff, ct))
            .OrderByDescending(entry => entry.OccurredAt)
            .ToArray();

        var aggregates = events
            .GroupBy(entry => new { entry.Code, entry.Provider, entry.Model })
            .Select(group =>
            {
                var latest = group.OrderByDescending(entry => entry.OccurredAt).First();
                return new ProviderFallbackAggregate(
                    latest.Code,
                    latest.Provider,
                    latest.Model,
                    group.Count(),
                    latest.OccurredAt,
                    latest.Message);
            })
            .OrderByDescending(item => item.LastOccurredAt)
            .ToArray();

        return Task.FromResult(new ProviderFallbackSummary(
            DefaultLookbackWindow,
            events.Length,
            events.FirstOrDefault()?.OccurredAt,
            aggregates));
    }

    private static IEnumerable<ProviderFallbackEvent> ReadFallbackEvents(
        string filePath,
        DateTimeOffset cutoff,
        CancellationToken ct)
    {
        foreach (var line in File.ReadLines(filePath))
        {
            ct.ThrowIfCancellationRequested();

            var match = WarningLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            if (!DateTimeOffset.TryParseExact(
                    match.Groups["timestamp"].Value,
                    "yyyy-MM-dd HH:mm:ss.fff zzz",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var occurredAt))
            {
                continue;
            }

            if (occurredAt.ToUniversalTime() < cutoff)
            {
                continue;
            }

            var message = match.Groups["message"].Value.Trim();
            if (!TryClassify(message, out var code))
            {
                continue;
            }

            yield return new ProviderFallbackEvent(
                occurredAt,
                code,
                ParseProvider(message),
                ParseModel(message),
                message);
        }
    }

    private static bool TryClassify(string message, out string code)
    {
        if (message.Contains("temporarily skipping remote requests until", StringComparison.OrdinalIgnoreCase))
        {
            code = "embedding_provider_cooldown_active";
            return true;
        }

        if (message.Contains("could not use the configured model because it is unavailable or unsupported", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("model_not_found", StringComparison.OrdinalIgnoreCase))
        {
            code = "ai_model_unavailable";
            return true;
        }

        if (message.Contains("hit a quota, token, or rate limit", StringComparison.OrdinalIgnoreCase))
        {
            code = "ai_quota_or_rate_limit";
            return true;
        }

        if (message.Contains("was rejected by the provider", StringComparison.OrdinalIgnoreCase))
        {
            code = "ai_authentication_failed";
            return true;
        }

        if (message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            code = "ai_request_timed_out";
            return true;
        }

        if (message.Contains("could not reach the provider endpoint", StringComparison.OrdinalIgnoreCase))
        {
            code = "ai_connection_failed";
            return true;
        }

        if (message.Contains("not fully configured", StringComparison.OrdinalIgnoreCase))
        {
            code = "embedding_provider_not_configured";
            return true;
        }

        if (message.Contains("is not recognized", StringComparison.OrdinalIgnoreCase))
        {
            code = "embedding_provider_unknown";
            return true;
        }

        if (message.Contains("Local deterministic embeddings were used", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("simple local summarizer was used", StringComparison.OrdinalIgnoreCase))
        {
            code = "ai_provider_failed";
            return true;
        }

        code = string.Empty;
        return false;
    }

    private static string? ParseProvider(string message)
    {
        var explicitConfigured = ConfiguredProviderRegex().Match(message);
        if (explicitConfigured.Success)
        {
            return explicitConfigured.Groups["provider"].Value;
        }

        var standard = StandardProviderRegex().Match(message);
        return standard.Success ? standard.Groups["provider"].Value : null;
    }

    private static string? ParseModel(string message)
    {
        var match = ModelRegex().Match(message);
        return match.Success ? match.Groups["model"].Value : null;
    }

    private static string BuildSearchPattern(string template)
    {
        var extension = Path.GetExtension(template);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(template);
        if (fileNameWithoutExtension.EndsWith("-", StringComparison.Ordinal))
        {
            return $"{fileNameWithoutExtension}*{extension}";
        }

        return $"*{extension}";
    }

    [GeneratedRegex(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[WRN\] (?<message>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex WarningLineRegex();

    [GeneratedRegex(@"^The (?<provider>.+?) (?:(?:embedding|summarization) request|embedding provider)", RegexOptions.CultureInvariant)]
    private static partial Regex StandardProviderRegex();

    [GeneratedRegex(@"^The configured embedding provider '(?<provider>[^']+)'", RegexOptions.CultureInvariant)]
    private static partial Regex ConfiguredProviderRegex();

    [GeneratedRegex(@"for model '(?<model>[^']+)'", RegexOptions.CultureInvariant)]
    private static partial Regex ModelRegex();
}

public sealed record ProviderFallbackSummary(
    TimeSpan LookbackWindow,
    int TotalRecentEvents,
    DateTimeOffset? LastOccurredAt,
    IReadOnlyList<ProviderFallbackAggregate> Aggregates);

public sealed record ProviderFallbackAggregate(
    string Code,
    string? Provider,
    string? Model,
    int Count,
    DateTimeOffset LastOccurredAt,
    string LatestMessage);

internal sealed record ProviderFallbackEvent(
    DateTimeOffset OccurredAt,
    string Code,
    string? Provider,
    string? Model,
    string Message);
