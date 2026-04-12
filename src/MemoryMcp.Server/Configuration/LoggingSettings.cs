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
}
