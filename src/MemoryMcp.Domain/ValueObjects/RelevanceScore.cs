namespace MemoryMcp.Domain.ValueObjects;

public sealed record RelevanceScore
{
    public double Value { get; }

    public RelevanceScore(double value)
    {
        if (value is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Relevance score must be between 0 and 1.");
        }

        Value = value;
    }

    public override string ToString() => Value.ToString("F4");
}
