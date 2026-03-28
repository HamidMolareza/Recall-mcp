namespace MemoryMcp.Server.Configuration;

public sealed class MemorySettings
{
    public const string SectionName = "Memory";

    public int? DefaultTtlDays { get; init; }
    public int MaxEntriesPerNamespace { get; init; } = 10_000;
    public int StalenessThresholdDays { get; init; } = 90;
    public bool EnableContradictionDetection { get; init; } = true;
    public bool EnableDeduplication { get; init; } = true;
}
