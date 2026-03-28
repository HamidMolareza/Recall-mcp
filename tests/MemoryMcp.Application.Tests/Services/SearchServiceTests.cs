using Xunit;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MemoryMcp.Application.Tests.Services;

public class SearchServiceTests
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<SearchService> _logger;
    private readonly SearchService _sut;

    public SearchServiceTests()
    {
        _repository = Substitute.For<IMemoryRepository>();
        _embeddingProvider = Substitute.For<IEmbeddingProvider>();
        _vectorStore = Substitute.For<IVectorStore>();
        _logger = Substitute.For<ILogger<SearchService>>();

        _sut = new SearchService(_repository, _embeddingProvider, _vectorStore, _logger);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        Assert.NotNull(_sut);
    }
}
