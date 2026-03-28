using MemoryMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMcp.Infrastructure.Persistence.Configurations;

public sealed class MemoryVersionConfiguration : IEntityTypeConfiguration<MemoryVersion>
{
    public void Configure(EntityTypeBuilder<MemoryVersion> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Content).IsRequired().HasMaxLength(50_000);
        builder.HasIndex(v => v.MemoryEntryId);
    }
}
