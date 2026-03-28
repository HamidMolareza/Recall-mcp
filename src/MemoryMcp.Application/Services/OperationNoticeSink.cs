using MemoryMcp.Application.Interfaces;

namespace MemoryMcp.Application.Services;

public sealed class OperationNoticeSink : IOperationNoticeSink
{
    private readonly List<OperationWarning> _warnings = [];

    public void AddWarning(OperationWarning warning)
    {
        ArgumentNullException.ThrowIfNull(warning);

        if (_warnings.Any(existing =>
                existing.Code == warning.Code &&
                existing.Message == warning.Message &&
                existing.SuggestedAction == warning.SuggestedAction &&
                existing.Details == warning.Details))
        {
            return;
        }

        _warnings.Add(warning);
    }

    public IReadOnlyList<OperationWarning> DrainWarnings()
    {
        if (_warnings.Count == 0)
        {
            return [];
        }

        var snapshot = _warnings.ToArray();
        _warnings.Clear();
        return snapshot;
    }
}
