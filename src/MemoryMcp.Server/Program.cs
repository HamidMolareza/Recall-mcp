using MemoryMcp.Application;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Infrastructure;
using MemoryMcp.Infrastructure.Persistence;
using MemoryMcp.Infrastructure.Runtime;
using MemoryMcp.Server.Configuration;
using MemoryMcp.Server.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MemoryMcp server");

    var builder = Host.CreateApplicationBuilder(args);
    EnsureBaseFolders(builder.Configuration);

    builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection(StorageSettings.SectionName));
    builder.Services.Configure<EmbeddingSettings>(builder.Configuration.GetSection(EmbeddingSettings.SectionName));
    builder.Services.Configure<AiSettings>(builder.Configuration.GetSection(AiSettings.SectionName));
    builder.Services.Configure<MemorySettings>(builder.Configuration.GetSection(MemorySettings.SectionName));
    builder.Services.Configure<LoggingSettings>(builder.Configuration.GetSection(LoggingSettings.SectionName));
    builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection(ServerSettings.SectionName));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(Program).Assembly);

    ConfigureSerilog(builder);

    var app = builder.Build();
    LogStartupConfiguration(builder.Configuration);
    await InitializePersistenceAsync(app.Services);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MemoryMcp server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static void ConfigureSerilog(HostApplicationBuilder builder)
{
    builder.Services.AddSerilog((services, config) =>
    {
        var loggingSection = builder.Configuration.GetSection(LoggingSettings.SectionName);
        var logPath = RuntimePathResolver.ResolvePath(loggingSection["LogPath"] ?? "app-data/logs");
        var generalFile = loggingSection["GeneralLogFile"] ?? "app-.log";
        var auditFile = loggingSection["AuditLogFile"] ?? "audit-.log";
        var errorFile = loggingSection["ErrorLogFile"] ?? "errors-.log";
        var retainedCount = loggingSection.GetValue<int?>("RetainedFileCountLimit") ?? 30;

        config
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logPath, generalFile),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedCount)
            .WriteTo.Logger(sub => sub
                .Filter.ByIncludingOnly("IsAudit = true")
                .WriteTo.File(
                    Path.Combine(logPath, auditFile),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedCount))
            .WriteTo.Logger(sub => sub
                .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Warning)
                .WriteTo.File(
                    Path.Combine(logPath, errorFile),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedCount));
    });
}

static async Task InitializePersistenceAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MemoryDbContext>();
    var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

    Log.Debug("Ensuring database is created at startup");

    await dbContext.Database.EnsureCreatedAsync();

    var memoriesWithEmbeddings = await dbContext.Memories
        .AsNoTracking()
        .Where(memory => memory.Embedding != null)
        .ToArrayAsync();

    Log.Information("Loaded {Count} persisted embeddings into the in-memory vector index", memoriesWithEmbeddings.Length);

    foreach (var memory in memoriesWithEmbeddings)
    {
        await vectorStore.UpsertAsync(memory.Id, memory.Embedding!, memory.Namespace.Value);
    }
}

static void EnsureBaseFolders(IConfiguration configuration)
{
    var logPath = configuration.GetSection(LoggingSettings.SectionName)["LogPath"];
    if (!string.IsNullOrWhiteSpace(logPath))
    {
        var fullLogPath = RuntimePathResolver.ResolvePath(logPath);
        Directory.CreateDirectory(fullLogPath);
    }

    var connectionString = configuration.GetSection(StorageSettings.SectionName)["ConnectionString"];
    var databasePath = RuntimePathResolver.TryExtractSqliteFilePath(connectionString);
    if (!string.IsNullOrWhiteSpace(databasePath))
    {
        var fullDatabasePath = RuntimePathResolver.ResolvePath(databasePath);
        var databaseDirectory = Path.GetDirectoryName(fullDatabasePath);

        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }
    }
}

static void LogStartupConfiguration(IConfiguration configuration)
{
    var rawConnectionString = configuration.GetSection(StorageSettings.SectionName)["ConnectionString"];
    var rawLogPath = configuration.GetSection(LoggingSettings.SectionName)["LogPath"];

    Log.Information(
        "Startup configuration: cwd={CurrentDirectory}, storage={ConnectionString}, logPath={LogPath}, embeddingProvider={EmbeddingProvider}, embeddingBaseUrl={EmbeddingBaseUrl}, aiProvider={AiProvider}, aiBaseUrl={AiBaseUrl}",
        Directory.GetCurrentDirectory(),
        rawConnectionString is null ? null : RuntimePathResolver.ResolveSqliteConnectionString(rawConnectionString),
        string.IsNullOrWhiteSpace(rawLogPath) ? null : RuntimePathResolver.ResolvePath(rawLogPath),
        configuration.GetSection(EmbeddingSettings.SectionName)["Provider"],
        configuration.GetSection(EmbeddingSettings.SectionName)["BaseUrl"],
        configuration.GetSection(AiSettings.SectionName)["Provider"],
        configuration.GetSection(AiSettings.SectionName)["BaseUrl"]);
}
