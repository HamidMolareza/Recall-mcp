using MemoryMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMcp.Infrastructure.Persistence.Configurations;

public sealed class MemoryLinkConfiguration : IEntityTypeConfiguration<MemoryLink>
{
    public void Configure(EntityTypeBuilder<MemoryLink> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Relation)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(l => l.Label)
            .HasMaxLength(200);

        builder.HasIndex(l => l.SourceId);
        builder.HasIndex(l => l.TargetId);
    }
}
