using MemoryMcp.Application.Interfaces;
using MemoryMcp.Infrastructure.Persistence;
using MemoryMcp.Infrastructure.Runtime;
using MemoryMcp.Infrastructure.VectorSearch;
using MemoryMcp.Server.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class RuntimeStatusTool
{
    private readonly IConfiguration _configuration;
    private readonly MemoryDbContext _dbContext;
    private readonly InMemoryVectorStore _vectorStore;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<RuntimeStatusTool> _logger;

    public RuntimeStatusTool(
        IConfiguration configuration,
        MemoryDbContext dbContext,
        InMemoryVectorStore vectorStore,
        IOperationNoticeSink operationNoticeSink,
        ILogger<RuntimeStatusTool> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _vectorStore = vectorStore;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "get_runtime_status"), Description("Reports resolved runtime paths, provider configuration, and current persistence counts.")]
    public async Task<object> GetRuntimeStatusAsync(CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("get_runtime_status", _logger, _operationNoticeSink, async innerCt =>
        {
            var rawConnectionString = _configuration.GetSection(StorageSettings.SectionName)["ConnectionString"]
                ?? "Data Source=app-data/memory.db";
            var resolvedConnectionString = RuntimePathResolver.ResolveSqliteConnectionString(rawConnectionString);
            var rawLogPath = _configuration.GetSection(LoggingSettings.SectionName)["LogPath"]
                ?? "app-data/logs";
            var resolvedLogPath = RuntimePathResolver.ResolvePath(rawLogPath);

            var memoryCount = await _dbContext.Memories.CountAsync(innerCt);
            var embeddedMemoryCount = await _dbContext.Memories.CountAsync(memory => memory.Embedding != null, innerCt);
            var vectorSnapshot = _vectorStore.GetSnapshot();

            return new RuntimeStatusData(
                Directory.GetCurrentDirectory(),
                new RuntimeStorageStatus(
                    rawConnectionString,
                    resolvedConnectionString,
                    RuntimePathResolver.TryExtractSqliteFilePath(resolvedConnectionString)),
                new RuntimeLoggingStatus(
                    rawLogPath,
                    resolvedLogPath,
                    _configuration.GetSection(LoggingSettings.SectionName)["GeneralLogFile"] ?? "app-.log",
                    _configuration.GetSection(LoggingSettings.SectionName)["AuditLogFile"] ?? "audit-.log",
                    _configuration.GetSection(LoggingSettings.SectionName)["ErrorLogFile"] ?? "errors-.log"),
                new RuntimeProviderStatus(
                    _configuration.GetSection(EmbeddingSettings.SectionName)["Provider"] ?? "Ollama",
                    _configuration.GetSection(EmbeddingSettings.SectionName)["Model"] ?? "nomic-embed-text",
                    _configuration.GetSection(EmbeddingSettings.SectionName)["BaseUrl"] ?? "http://localhost:11434",
                    _configuration.GetSection(AiSettings.SectionName)["Provider"] ?? "Simple",
                    _configuration.GetSection(AiSettings.SectionName)["Model"] ?? "gapgpt-qwen-3.5",
                    _configuration.GetSection(AiSettings.SectionName)["BaseUrl"] ?? "https://api.gapgpt.app/v1"),
                new RuntimePersistenceStatus(memoryCount, embeddedMemoryCount),
                new RuntimeVectorIndexStatus(
                    vectorSnapshot.EntryCount,
                    vectorSnapshot.NamespacedEntryCount,
                    vectorSnapshot.UnscopedEntryCount,
                    vectorSnapshot.NamespaceCounts));
        }, ct);
    }
}

public sealed record RuntimeStatusData(
    string CurrentWorkingDirectory,
    RuntimeStorageStatus Storage,
    RuntimeLoggingStatus Logging,
    RuntimeProviderStatus Providers,
    RuntimePersistenceStatus Persistence,
    RuntimeVectorIndexStatus VectorIndex);

public sealed record RuntimeStorageStatus(
    string RawConnectionString,
    string ResolvedConnectionString,
    string? ResolvedDatabasePath);

public sealed record RuntimeLoggingStatus(
    string RawLogPath,
    string ResolvedLogPath,
    string GeneralLogFile,
    string AuditLogFile,
    string ErrorLogFile);

public sealed record RuntimeProviderStatus(
    string EmbeddingProvider,
    string EmbeddingModel,
    string EmbeddingBaseUrl,
    string AiProvider,
    string AiModel,
    string AiBaseUrl);

public sealed record RuntimePersistenceStatus(
    int MemoryCount,
    int EmbeddedMemoryCount);

public sealed record RuntimeVectorIndexStatus(
    int EntryCount,
    int NamespacedEntryCount,
    int UnscopedEntryCount,
    IReadOnlyDictionary<string, int> NamespaceCounts);
