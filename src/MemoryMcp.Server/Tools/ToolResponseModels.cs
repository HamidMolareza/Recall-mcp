using MemoryMcp.Application.Interfaces;

namespace MemoryMcp.Server.Tools;

public sealed record ToolResponse
{
    public required bool Ok { get; init; }
    public object? Data { get; init; }
    public IReadOnlyList<OperationWarning> Warnings { get; init; } = [];
    public ToolError? Error { get; init; }

    public static ToolResponse Success(object? data, IReadOnlyList<OperationWarning> warnings) => new()
    {
        Ok = true,
        Data = data,
        Warnings = warnings
    };

    public static ToolResponse Failure(ToolError error, IReadOnlyList<OperationWarning> warnings) => new()
    {
        Ok = false,
        Error = error,
        Warnings = warnings
    };
}

public sealed record ToolError(
    string Code,
    string Message,
    string SuggestedAction,
    string? Details = null);
