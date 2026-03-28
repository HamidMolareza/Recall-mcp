namespace MemoryMcp.Server.Configuration;

public sealed class AiSettings
{
    public const string SectionName = "AI";

    public string Provider { get; init; } = "Simple";
    public string Model { get; init; } = "gapgpt-qwen-3.5";
    public string BaseUrl { get; init; } = "https://api.gapgpt.app/v1";
    public string ApiKey { get; init; } = string.Empty;
}
