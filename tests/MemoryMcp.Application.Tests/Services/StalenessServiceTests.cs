using FluentAssertions;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Entities;
using MemoryMcp.Domain.Enums;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MemoryMcp.Application.Tests.Services;

public sealed class StalenessServiceTests
{
    private readonly IMemoryRepository _repository;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<StalenessService> _logger;
    private readonly StalenessService _sut;

    public StalenessServiceTests()
    {
        _repository = Substitute.For<IMemoryRepository>();
        _vectorStore = Substitute.For<IVectorStore>();
        _logger = Substitute.For<ILogger<StalenessService>>();
        _sut = new StalenessService(_repository, _vectorStore, _logger);
    }

    [Fact]
    public async Task CleanupExpiredAsync_DeletesExpiredMemoriesAndRequestsExpiredRows()
    {
        var expired = MemoryEntry.Create(
            "Expired",
            MemoryType.Fact,
            new Namespace("cleanup"),
            expiresAt: DateTime.UtcNow.AddDays(-2));
        var active = MemoryEntry.Create(
            "Active",
            MemoryType.Context,
            new Namespace("cleanup"),
            expiresAt: DateTime.UtcNow.AddDays(2));

        _repository.ListAsync(
                Arg.Is<MemoryQueryFilter>(filter => filter.IncludeExpired && filter.PageSize == 10_000),
                Arg.Any<CancellationToken>())
            .Returns([expired, active]);

        var deletedCount = await _sut.CleanupExpiredAsync();

        deletedCount.Should().Be(1);
        await _repository.Received(1).DeleteAsync(expired.Id, Arg.Any<CancellationToken>());
        await _vectorStore.Received(1).DeleteAsync(expired.Id, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().DeleteAsync(active.Id, Arg.Any<CancellationToken>());
    }
}
