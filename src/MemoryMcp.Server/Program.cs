using MemoryMcp.Application;
using MemoryMcp.Infrastructure;
using MemoryMcp.Server.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MemoryMcp server");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection(StorageSettings.SectionName));
    builder.Services.Configure<EmbeddingSettings>(builder.Configuration.GetSection(EmbeddingSettings.SectionName));
    builder.Services.Configure<MemorySettings>(builder.Configuration.GetSection(MemorySettings.SectionName));
    builder.Services.Configure<LoggingSettings>(builder.Configuration.GetSection(LoggingSettings.SectionName));
    builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection(ServerSettings.SectionName));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    ConfigureSerilog(builder);

    var app = builder.Build();
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
        var logPath = loggingSection["LogPath"] ?? "./logs";
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
