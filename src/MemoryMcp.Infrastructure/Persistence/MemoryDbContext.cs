using MemoryMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemoryMcp.Infrastructure.Persistence;

public sealed class MemoryDbContext : DbContext
{
    public DbSet<MemoryEntry> Memories => Set<MemoryEntry>();
    public DbSet<MemoryVersion> Versions => Set<MemoryVersion>();
    public DbSet<MemoryLink> Links => Set<MemoryLink>();
    public DbSet<Tag> Tags => Set<Tag>();

    public MemoryDbContext(DbContextOptions<MemoryDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MemoryDbContext).Assembly);
    }
}
