using MemoryMcp.Application.DTOs;
using MemoryMcp.Domain.Entities;

namespace MemoryMcp.Application.Services;

internal static class MemoryResponseMapper
{
    public static MemoryResponse ToResponse(this MemoryEntry entry, double? relevanceScore = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new MemoryResponse
        {
            Id = entry.Id,
            Content = entry.Content,
            Type = entry.Type,
            Namespace = entry.Namespace.Value,
            Tags = entry.Tags.Select(tag => tag.Label).ToArray(),
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            LastAccessedAt = entry.LastAccessedAt,
            ExpiresAt = entry.ExpiresAt,
            RelevanceScore = relevanceScore
        };
    }
}
