using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Domain.Entities;

public sealed class MemoryLink
{
    public Guid Id { get; private set; }
    public Guid SourceId { get; private set; }
    public Guid TargetId { get; private set; }
    public LinkRelation Relation { get; private set; }
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private MemoryLink() { }

    public static MemoryLink Create(Guid sourceId, Guid targetId, LinkRelation relation, string? label = null)
    {
        if (sourceId == targetId)
        {
            throw new ArgumentException("A memory cannot link to itself.");
        }

        return new MemoryLink
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            Relation = relation,
            Label = label,
            CreatedAt = DateTime.UtcNow
        };
    }
}
