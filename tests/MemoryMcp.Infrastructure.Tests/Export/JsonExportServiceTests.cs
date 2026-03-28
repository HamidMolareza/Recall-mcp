using FluentAssertions;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Export;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MemoryMcp.Infrastructure.Tests.Export;

public class JsonExportServiceTests
{
    [Fact]
    public async Task ExportToJsonAndImportFromJson_RoundTripsCoreMemoryFields()
    {
        var logger = Substitute.For<ILogger<JsonExportService>>();
        var service = new JsonExportService(logger);

        var entry = MemoryEntry.Create(
            "Original content",
            MemoryType.Fact,
            new Namespace("work/project-x"),
            ["alpha", "beta"]);
        entry.UpdateContent("Updated content");
        entry.AddLink(MemoryLink.Create(entry.Id, Guid.NewGuid(), LinkRelation.RelatedTo, "reference"));
        entry.SetEmbedding(new EmbeddingVector([1.0f, 2.0f, 3.0f]));

        var json = await service.ExportToJsonAsync([entry]);
        var imported = await service.ImportFromJsonAsync(json);

        imported.Should().HaveCount(1);
        imported[0].Id.Should().Be(entry.Id);
        imported[0].Content.Should().Be(entry.Content);
        imported[0].Namespace.Value.Should().Be(entry.Namespace.Value);
        imported[0].Tags.Select(tag => tag.Label).Should().BeEquivalentTo(entry.Tags.Select(tag => tag.Label));
        imported[0].Versions.Should().HaveCount(1);
        imported[0].OutgoingLinks.Should().HaveCount(1);
        imported[0].Embedding!.Values.Should().Equal(entry.Embedding!.Values);
    }
}
