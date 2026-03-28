using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMcp.Infrastructure.Persistence.Configurations;

public sealed class MemoryEntryConfiguration : IEntityTypeConfiguration<MemoryEntry>
{
    public void Configure(EntityTypeBuilder<MemoryEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Content)
            .IsRequired()
            .HasMaxLength(50_000);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.Namespace)
            .IsRequired()
            .HasMaxLength(500)
            .HasConversion(
                ns => ns.Value,
                value => new Namespace(value));

        builder.Property(e => e.Embedding)
            .HasConversion(
                v => v == null ? null : ConvertToBytes(v),
                b => b == null ? null : ConvertToVector(b));

        builder.HasMany(e => e.Tags)
            .WithOne()
            .HasForeignKey(t => t.MemoryEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Versions)
            .WithOne()
            .HasForeignKey(v => v.MemoryEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.OutgoingLinks)
            .WithOne()
            .HasForeignKey(l => l.SourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Namespace);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.ExpiresAt);
    }

    private static byte[] ConvertToBytes(EmbeddingVector vector)
    {
        var bytes = new byte[vector.Values.Length * sizeof(float)];
        Buffer.BlockCopy(vector.Values, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static EmbeddingVector ConvertToVector(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return new EmbeddingVector(floats);
    }
}
