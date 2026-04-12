using Serilog;
using Serilog.Events;

namespace MemoryMcp.Server.Configuration;

public static class SerilogLevelConfigurator
{
    public static LoggerConfiguration Apply(LoggerConfiguration configuration, LoggingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(settings);

        var defaultLevel = ParseLevel(settings.MinimumLevel.Default, LogEventLevel.Information);
        configuration.MinimumLevel.Is(defaultLevel);

        foreach (var entry in settings.MinimumLevel.Override)
        {
            configuration.MinimumLevel.Override(entry.Key, ParseLevel(entry.Value, defaultLevel));
        }

        return configuration;
    }

    private static LogEventLevel ParseLevel(string? value, LogEventLevel fallback)
    {
        return Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var parsedLevel)
            ? parsedLevel
            : fallback;
    }
}
