using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MemoryMcp.Infrastructure.Export;

public sealed class JsonExportService : IExportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<JsonExportService> _logger;

    public JsonExportService(ILogger<JsonExportService> logger)
    {
        _logger = logger;
    }

    public Task<string> ExportToJsonAsync(IEnumerable<MemoryEntry> entries, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ct.ThrowIfCancellationRequested();

        var payload = entries.Select(ExportMemoryModel.FromDomain).ToArray();
        var json = JsonSerializer.Serialize(payload, SerializerOptions);

        _logger.LogInformation("{IsAudit} Exported {Count} memories to JSON", true, payload.Length);
        return Task.FromResult(json);
    }

    public Task<string> ExportToMarkdownAsync(IEnumerable<MemoryEntry> entries, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ct.ThrowIfCancellationRequested();

        var builder = new StringBuilder();
        foreach (var entry in entries.OrderBy(memory => memory.Namespace.Value).ThenBy(memory => memory.CreatedAt))
        {
            builder.AppendLine($"## {entry.Namespace.Value} / {entry.Type} / {entry.Id}");
            builder.AppendLine();
            builder.AppendLine(entry.Content);
            builder.AppendLine();

            if (entry.Tags.Count > 0)
            {
                builder.AppendLine($"- Tags: {string.Join(", ", entry.Tags.Select(tag => tag.Label))}");
            }

            builder.AppendLine($"- Created: {entry.CreatedAt:O}");
            builder.AppendLine($"- Updated: {entry.UpdatedAt:O}");

            if (entry.ExpiresAt.HasValue)
            {
                builder.AppendLine($"- Expires: {entry.ExpiresAt:O}");
            }

            if (entry.Versions.Count > 0)
            {
                builder.AppendLine("- Versions:");
                foreach (var version in entry.Versions.OrderByDescending(version => version.CreatedAt))
                {
                    builder.AppendLine($"  - {version.CreatedAt:O}: {version.Content}");
                }
            }

            if (entry.OutgoingLinks.Count > 0)
            {
                builder.AppendLine("- Links:");
                foreach (var link in entry.OutgoingLinks)
                {
                    builder.AppendLine($"  - {link.Relation} => {link.TargetId} ({link.Label ?? "no-label"})");
                }
            }

            builder.AppendLine();
        }

        var markdown = builder.ToString().Trim();
        _logger.LogInformation("{IsAudit} Exported memories to Markdown", true);
        return Task.FromResult(markdown);
    }

    public Task<IReadOnlyList<MemoryEntry>> ImportFromJsonAsync(string json, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ct.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Deserialize<ExportMemoryModel[]>(json, SerializerOptions) ?? [];
        var entries = payload.Select(ToDomain).ToArray();

        _logger.LogInformation("{IsAudit} Imported {Count} memories from JSON", true, entries.Length);
        return Task.FromResult<IReadOnlyList<MemoryEntry>>(entries);
    }

    private static MemoryEntry ToDomain(ExportMemoryModel model)
    {
        var entry = MemoryEntry.Create(
            model.Content,
            model.Type,
            new Namespace(model.Namespace),
            model.Tags,
            model.ExpiresAt);

        Set(entry, nameof(MemoryEntry.Id), model.Id);
        Set(entry, nameof(MemoryEntry.CreatedAt), model.CreatedAt);
        Set(entry, nameof(MemoryEntry.UpdatedAt), model.UpdatedAt);
        Set(entry, nameof(MemoryEntry.LastAccessedAt), model.LastAccessedAt);

        if (model.Embedding is { Length: > 0 })
        {
            entry.SetEmbedding(new EmbeddingVector(model.Embedding));
        }

        var versions = GetBackingList<MemoryVersion>(entry, "_versions");
        versions.Clear();
        foreach (var versionModel in model.Versions)
        {
            var version = MemoryVersion.Create(versionModel.Content, versionModel.CreatedAt);
            Set(version, nameof(MemoryVersion.Id), versionModel.Id);
            versions.Add(version);
        }

        var links = GetBackingList<MemoryLink>(entry, "_outgoingLinks");
        links.Clear();
        foreach (var linkModel in model.Links)
        {
            var link = MemoryLink.Create(model.Id, linkModel.TargetId, linkModel.Relation, linkModel.Label);
            Set(link, nameof(MemoryLink.Id), linkModel.Id);
            Set(link, nameof(MemoryLink.CreatedAt), linkModel.CreatedAt);
            links.Add(link);
        }

        return entry;
    }

    private static List<TItem> GetBackingList<TItem>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field '{fieldName}' was not found.");
        return (List<TItem>)(field.GetValue(target)
            ?? throw new InvalidOperationException($"Backing field '{fieldName}' is null."));
    }

    private static void Set<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found.");
        property.SetValue(target, value);
    }

    private sealed record ExportMemoryModel(
        Guid Id,
        string Content,
        Domain.Enums.MemoryType Type,
        string Namespace,
        string[] Tags,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime LastAccessedAt,
        DateTime? ExpiresAt,
        float[]? Embedding,
        ExportMemoryVersionModel[] Versions,
        ExportMemoryLinkModel[] Links)
    {
        public static ExportMemoryModel FromDomain(MemoryEntry entry) => new(
            entry.Id,
            entry.Content,
            entry.Type,
            entry.Namespace.Value,
            entry.Tags.Select(tag => tag.Label).ToArray(),
            entry.CreatedAt,
            entry.UpdatedAt,
            entry.LastAccessedAt,
            entry.ExpiresAt,
            entry.Embedding?.Values,
            entry.Versions
                .Select(version => new ExportMemoryVersionModel(version.Id, version.Content, version.CreatedAt))
                .ToArray(),
            entry.OutgoingLinks
                .Select(link => new ExportMemoryLinkModel(link.Id, link.TargetId, link.Relation, link.Label, link.CreatedAt))
                .ToArray());
    }

    private sealed record ExportMemoryVersionModel(Guid Id, string Content, DateTime CreatedAt);

    private sealed record ExportMemoryLinkModel(
        Guid Id,
        Guid TargetId,
        Domain.Enums.LinkRelation Relation,
        string? Label,
        DateTime CreatedAt);
}
