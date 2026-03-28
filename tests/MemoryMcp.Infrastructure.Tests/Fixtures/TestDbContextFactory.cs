using MemoryMcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MemoryMcp.Infrastructure.Tests.Fixtures;

public sealed class TestDbContextFactory : IDisposable
{
    private readonly DbContextOptions<MemoryDbContext> _options;

    public TestDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<MemoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    public MemoryDbContext CreateContext()
    {
        return new MemoryDbContext(_options);
    }

    public void Dispose()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }
}
