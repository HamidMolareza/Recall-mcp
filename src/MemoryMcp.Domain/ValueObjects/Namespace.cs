namespace MemoryMcp.Domain.ValueObjects;

public sealed record Namespace
{
    public string Value { get; }

    public Namespace(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToLowerInvariant();
    }

    public bool IsChildOf(Namespace parent)
    {
        return Value.StartsWith(parent.Value + "/", StringComparison.Ordinal);
    }

    public override string ToString() => Value;
}
