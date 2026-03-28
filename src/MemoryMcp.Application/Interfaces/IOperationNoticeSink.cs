namespace MemoryMcp.Application.Interfaces;

public interface IOperationNoticeSink
{
    void AddWarning(OperationWarning warning);
    IReadOnlyList<OperationWarning> DrainWarnings();
}

public sealed record OperationWarning(
    string Code,
    string Message,
    string SuggestedAction,
    string? Details = null);
