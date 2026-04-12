using FluentAssertions;
using MemoryMcp.Infrastructure.Runtime;
using Xunit;

namespace MemoryMcp.Infrastructure.Tests.Runtime;

public sealed class RuntimePathResolverTests
{
    [Fact]
    public void ResolvePath_WithRelativeAppDataPath_ReturnsAbsolutePath()
    {
        var resolvedPath = RuntimePathResolver.ResolvePath("app-data/logs");

        Path.IsPathRooted(resolvedPath).Should().BeTrue();
        resolvedPath.Should().EndWith(Path.Combine("app-data", "logs"));
    }

    [Fact]
    public void ResolveSqliteConnectionString_WithRelativePath_ReturnsAbsolutePath()
    {
        var resolvedConnectionString = RuntimePathResolver.ResolveSqliteConnectionString("Data Source=app-data/memory.db");
        var resolvedPath = RuntimePathResolver.TryExtractSqliteFilePath(resolvedConnectionString);

        resolvedPath.Should().NotBeNull();
        Path.IsPathRooted(resolvedPath!).Should().BeTrue();
        resolvedPath.Should().EndWith(Path.Combine("app-data", "memory.db"));
    }

    [Fact]
    public void ResolveSqliteConnectionString_WithAbsolutePath_KeepsOriginalValue()
    {
        var absolutePath = Path.Combine(Path.GetTempPath(), "memorymcp-tests", "memory.db");
        var connectionString = $"Data Source={absolutePath}";

        RuntimePathResolver.ResolveSqliteConnectionString(connectionString)
            .Should().Be(connectionString);
    }
}
