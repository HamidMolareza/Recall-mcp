namespace MemoryMcp.Domain.ValueObjects;

public sealed record EmbeddingVector
{
    public float[] Values { get; }
    public int Dimensions => Values.Length;

    public EmbeddingVector(float[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 0)
        {
            throw new ArgumentException("Embedding vector must not be empty.", nameof(values));
        }

        Values = values;
    }

    public double CosineSimilarity(EmbeddingVector other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Dimensions != other.Dimensions)
        {
            throw new ArgumentException("Vectors must have the same dimensions.", nameof(other));
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < Dimensions; i++)
        {
            dot += Values[i] * other.Values[i];
            normA += Values[i] * Values[i];
            normB += other.Values[i] * other.Values[i];
        }

        var denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator == 0 ? 0 : dot / denominator;
    }
}
