using MemoryMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMcp.Infrastructure.Persistence.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Label).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Label);
        builder.HasIndex(t => t.MemoryEntryId);
    }
}
