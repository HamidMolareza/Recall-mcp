using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Domain.Entities;

public sealed class MemoryEntry
{
    public Guid Id { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public MemoryType Type { get; private set; }
    public Namespace Namespace { get; private set; } = null!;
    public EmbeddingVector? Embedding { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime LastAccessedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    private readonly List<MemoryVersion> _versions = [];
    public IReadOnlyList<MemoryVersion> Versions => _versions.AsReadOnly();

    private readonly List<MemoryLink> _outgoingLinks = [];
    public IReadOnlyList<MemoryLink> OutgoingLinks => _outgoingLinks.AsReadOnly();

    private MemoryEntry() { }

    public static MemoryEntry Create(
        string content,
        MemoryType type,
        Namespace ns,
        IEnumerable<string>? tags = null,
        DateTime? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(ns);

        var now = DateTime.UtcNow;
        var entry = new MemoryEntry
        {
            Id = Guid.NewGuid(),
            Content = content,
            Type = type,
            Namespace = ns,
            CreatedAt = now,
            UpdatedAt = now,
            LastAccessedAt = now,
            ExpiresAt = expiresAt
        };

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                entry.AddTag(tag);
            }
        }

        return entry;
    }

    public void UpdateContent(string newContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newContent);

        _versions.Add(MemoryVersion.Create(Content, UpdatedAt));
        Content = newContent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEmbedding(EmbeddingVector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        Embedding = vector;
    }

    public void AddTag(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        var normalized = label.Trim().ToLowerInvariant();
        if (_tags.All(t => t.Label != normalized))
        {
            _tags.Add(Tag.Create(normalized));
        }
    }

    public void RemoveTag(string label)
    {
        var normalized = label.Trim().ToLowerInvariant();
        _tags.RemoveAll(t => t.Label == normalized);
    }

    public void AddLink(MemoryLink link)
    {
        ArgumentNullException.ThrowIfNull(link);
        _outgoingLinks.Add(link);
    }

    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
    }

    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    public bool IsStale(int thresholdDays) =>
        LastAccessedAt < DateTime.UtcNow.AddDays(-thresholdDays);
}
