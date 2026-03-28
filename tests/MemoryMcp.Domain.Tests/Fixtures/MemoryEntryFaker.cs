using Bogus;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Domain.Tests.Fixtures;

public static class MemoryEntryFaker
{
    private static readonly Faker Faker = new();

    public static MemoryEntry CreateValid(
        string? content = null,
        MemoryType? type = null,
        string? ns = null,
        IEnumerable<string>? tags = null,
        DateTime? expiresAt = null)
    {
        return MemoryEntry.Create(
            content ?? Faker.Lorem.Sentence(),
            type ?? Faker.PickRandom<MemoryType>(),
            new Namespace(ns ?? Faker.Internet.DomainWord()),
            tags ?? [Faker.Lorem.Word(), Faker.Lorem.Word()],
            expiresAt);
    }
}
