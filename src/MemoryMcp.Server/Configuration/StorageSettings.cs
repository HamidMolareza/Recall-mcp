namespace MemoryMcp.Server.Configuration;

public sealed class StorageSettings
{
    public const string SectionName = "Storage";

    public string Provider { get; init; } = "Sqlite";
    public string ConnectionString { get; init; } = "Data Source=app-data/memory.db";
}
