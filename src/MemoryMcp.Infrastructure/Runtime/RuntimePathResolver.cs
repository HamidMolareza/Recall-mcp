namespace MemoryMcp.Infrastructure.Runtime;

public static class RuntimePathResolver
{
    public static string ResolvePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, ResolveRuntimeRoot());
    }

    public static string ResolveSqliteConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var sqlitePath = TryExtractSqliteFilePath(connectionString);
        if (string.IsNullOrWhiteSpace(sqlitePath) ||
            sqlitePath.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
            Path.IsPathRooted(sqlitePath))
        {
            return connectionString;
        }

        var resolvedPath = ResolvePath(sqlitePath);
        var segments = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => RewriteDataSourceSegment(segment, resolvedPath));

        return string.Join(";", segments);
    }

    public static string? TryExtractSqliteFilePath(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim().Trim('"');
            if (key.Equals("Data Source", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("DataSource", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    private static string RewriteDataSourceSegment(string segment, string resolvedPath)
    {
        var separatorIndex = segment.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return segment;
        }

        var key = segment[..separatorIndex].Trim();
        return key.Equals("Data Source", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("DataSource", StringComparison.OrdinalIgnoreCase)
            ? $"{key}={resolvedPath}"
            : segment.Trim();
    }

    private static string ResolveRuntimeRoot()
    {
        foreach (var root in EnumerateCandidateRoots())
        {
            if (File.Exists(Path.Combine(root, "MemoryMcp.slnx")) ||
                Directory.Exists(Path.Combine(root, "app-data")))
            {
                return root;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static IEnumerable<string> EnumerateCandidateRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (seen.Add(directory.FullName))
                {
                    yield return directory.FullName;
                }

                directory = directory.Parent;
            }
        }
    }
}
