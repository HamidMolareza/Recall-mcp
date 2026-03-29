using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Server.Tools;

public static class MemoryTypeArgumentParser
{
    public const string AllowedValuesDescription = "Allowed values: Fact, Preference, Context, Instruction, Episode, Entity. Values are case-insensitive.";

    private static readonly string[] AllowedValues = Enum.GetNames<MemoryType>();

    public static MemoryType ParseRequired(string value, string parameterName)
    {
        if (TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw CreateException(parameterName, value);
    }

    public static MemoryType? ParseOptional(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw CreateException(parameterName, value);
    }

    public static MemoryType ParseOptionalOrDefault(string? value, MemoryType defaultValue, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw CreateException(parameterName, value);
    }

    private static bool TryParse(string value, out MemoryType parsed)
    {
        var matchedName = AllowedValues.FirstOrDefault(candidate =>
            string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase));

        if (matchedName is null)
        {
            parsed = default;
            return false;
        }

        parsed = Enum.Parse<MemoryType>(matchedName);
        return true;
    }

    private static ArgumentException CreateException(string parameterName, string? attemptedValue)
    {
        var renderedValue = string.IsNullOrWhiteSpace(attemptedValue) ? "<empty>" : attemptedValue;
        return new ArgumentException(
            $"'{parameterName}' must be one of: {string.Join(", ", AllowedValues)}. Received '{renderedValue}'.",
            parameterName);
    }
}
