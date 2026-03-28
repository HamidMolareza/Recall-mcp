namespace MemoryMcp.Domain.Exceptions;

public sealed class DuplicateMemoryException : Exception
{
    public Guid ExistingMemoryId { get; }

    public DuplicateMemoryException(Guid existingMemoryId)
        : base($"A near-duplicate memory entry already exists with ID '{existingMemoryId}'.")
    {
        ExistingMemoryId = existingMemoryId;
    }
}
