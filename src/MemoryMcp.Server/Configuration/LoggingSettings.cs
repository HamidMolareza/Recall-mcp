namespace MemoryMcp.Server.Configuration;

public sealed class LoggingSettings
{
    public const string SectionName = "Logging";

    public string LogPath { get; init; } = "app-data/logs";
    public string AuditLogFile { get; init; } = "audit-.log";
    public string ErrorLogFile { get; init; } = "errors-.log";
    public string GeneralLogFile { get; init; } = "app-.log";
    public string RollingInterval { get; init; } = "Day";
    public int RetainedFileCountLimit { get; init; } = 30;
    public LoggingMinimumLevelSettings MinimumLevel { get; init; } = new();
}

public sealed class LoggingMinimumLevelSettings
{
    public string Default { get; init; } = "Information";
    public Dictionary<string, string> Override { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft"] = "Warning",
        ["System"] = "Warning"
    };
}
