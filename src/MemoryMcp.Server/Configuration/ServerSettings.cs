namespace MemoryMcp.Server.Configuration;

public sealed class ServerSettings
{
    public const string SectionName = "Server";

    public string Transport { get; init; } = "stdio";
}
