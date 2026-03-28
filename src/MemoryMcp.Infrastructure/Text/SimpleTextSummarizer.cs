using System.Text.RegularExpressions;
using MemoryMcp.Application.Interfaces;

namespace MemoryMcp.Infrastructure.Text;

public sealed class SimpleTextSummarizer : ITextSummarizer
{
    public Task<string> SummarizeAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ct.ThrowIfCancellationRequested();

        var sentences = Regex.Split(text.Trim(), @"(?<=[.!?])\s+")
            .Where(static sentence => !string.IsNullOrWhiteSpace(sentence))
            .ToArray();

        if (sentences.Length <= 3)
        {
            return Task.FromResult(string.Join(" ", sentences));
        }

        var frequencies = sentences
            .SelectMany(Tokenize)
            .GroupBy(static token => token, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var summary = sentences
            .Select((sentence, index) => new
            {
                sentence,
                index,
                score = Tokenize(sentence).Sum(token => frequencies.GetValueOrDefault(token))
            })
            .OrderByDescending(item => item.score)
            .ThenBy(item => item.index)
            .Take(3)
            .OrderBy(item => item.index)
            .Select(item => item.sentence.Trim());

        return Task.FromResult(string.Join(" ", summary));
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return text
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '/', '\\', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static token => token.Length > 2);
    }
}
