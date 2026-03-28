namespace MemoryMcp.Domain.Entities;

public sealed class Tag
{
    public Guid Id { get; private set; }
    public string Label { get; private set; } = string.Empty;

    public Guid MemoryEntryId { get; private set; }

    private Tag() { }

    public static Tag Create(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        return new Tag
        {
            Id = Guid.NewGuid(),
            Label = label.Trim().ToLowerInvariant()
        };
    }
}
