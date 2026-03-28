namespace MemoryMcp.Domain.Exceptions;

public sealed class MemoryNotFoundException : Exception
{
    public Guid MemoryId { get; }

    public MemoryNotFoundException(Guid memoryId)
        : base($"Memory entry with ID '{memoryId}' was not found.")
    {
        MemoryId = memoryId;
    }
}
