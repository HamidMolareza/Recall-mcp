using Xunit;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MemoryMcp.Application.Tests.Services;

public class MemoryServiceTests
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<MemoryService> _logger;
    private readonly MemoryService _sut;

    public MemoryServiceTests()
    {
        _repository = Substitute.For<IMemoryRepository>();
        _embeddingProvider = Substitute.For<IEmbeddingProvider>();
        _vectorStore = Substitute.For<IVectorStore>();
        _logger = Substitute.For<ILogger<MemoryService>>();

        _sut = new MemoryService(_repository, _embeddingProvider, _vectorStore, _logger);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act covered by constructor in setup
        // Assert — instance was created
        Assert.NotNull(_sut);
    }
}
