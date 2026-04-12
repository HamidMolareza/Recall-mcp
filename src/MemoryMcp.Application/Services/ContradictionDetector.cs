using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Application.Services;

public sealed class ContradictionDetector
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<ContradictionDetector> _logger;

    public ContradictionDetector(
        IMemoryRepository repository,
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        ILogger<ContradictionDetector> logger)
    {
        _repository = repository;
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> DetectContradictionsAsync(
        string content,
        string ns,
        EmbeddingVector? embedding = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);

        var normalizedNamespace = ns.Trim().ToLowerInvariant();
        var vector = embedding ?? await _embeddingProvider.GenerateEmbeddingAsync(content, ct);
        var candidates = await _vectorStore.SearchAsync(vector, 25, ct);

        var contradictions = new List<Guid>();
        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();

            var entry = await _repository.GetByIdAsync(candidate.MemoryId, ct);
            if (entry is null || entry.IsExpired())
            {
                continue;
            }

            if (entry.Namespace.Value != normalizedNamespace &&
                !entry.Namespace.Value.StartsWith($"{normalizedNamespace}/", StringComparison.Ordinal))
            {
                continue;
            }

            if (LooksContradictory(content, entry.Content))
            {
                contradictions.Add(entry.Id);
            }
        }

        _logger.LogDebug("Contradiction detection found {Count} candidates", contradictions.Count);
        return contradictions;
    }

    private static bool LooksContradictory(string left, string right)
    {
        var leftTokens = Tokenize(left);
        var rightTokens = Tokenize(right);

        var overlap = leftTokens.Intersect(rightTokens).Count();
        if (overlap < 2)
        {
            return false;
        }

        var leftNegative = ContainsNegativeCue(leftTokens);
        var rightNegative = ContainsNegativeCue(rightTokens);
        if (leftNegative != rightNegative)
        {
            return true;
        }

        return (leftTokens.Contains("true") && rightTokens.Contains("false")) ||
               (leftTokens.Contains("false") && rightTokens.Contains("true"));
    }

    private static bool ContainsNegativeCue(HashSet<string> tokens) =>
        tokens.Overlaps(["not", "never", "no", "cannot", "can't", "wont", "won't", "false"]);

    private static HashSet<string> Tokenize(string text)
    {
        return text
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '/', '\\', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim().ToLowerInvariant())
            .Where(token => token.Length > 2)
            .ToHashSet(StringComparer.Ordinal);
    }
}
