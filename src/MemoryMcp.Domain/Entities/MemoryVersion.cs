namespace MemoryMcp.Domain.Entities;

public sealed class MemoryVersion
{
    public Guid Id { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public Guid MemoryEntryId { get; private set; }

    private MemoryVersion() { }

    public static MemoryVersion Create(string content, DateTime timestamp)
    {
        return new MemoryVersion
        {
            Id = Guid.NewGuid(),
            Content = content,
            CreatedAt = timestamp
        };
    }
}
