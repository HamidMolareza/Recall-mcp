using FluentAssertions;
using MemoryMcp.Server.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace MemoryMcp.Server.Tests.Configuration;

public sealed class SerilogLevelConfiguratorTests
{
    [Fact]
    public void Apply_UsesConfiguredDefaultAndOverrides()
    {
        var sink = new CollectingSink();
        var settings = new LoggingSettings
        {
            MinimumLevel = new LoggingMinimumLevelSettings
            {
                Default = "Information",
                Override = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Microsoft"] = "Warning"
                }
            }
        };

        using var logger = SerilogLevelConfigurator
            .Apply(new LoggerConfiguration(), settings)
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.ForContext(Constants.SourceContextPropertyName, "MemoryMcp.Server.Test")
            .Information("app info");
        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.EntityFrameworkCore.Database.Command")
            .Information("db info");
        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.EntityFrameworkCore.Database.Command")
            .Warning("db warning");

        sink.Events.Should().ContainSingle(evt => evt.Level == LogEventLevel.Information && evt.MessageTemplate.Text == "app info");
        sink.Events.Should().NotContain(evt => evt.Level == LogEventLevel.Information && evt.MessageTemplate.Text == "db info");
        sink.Events.Should().ContainSingle(evt => evt.Level == LogEventLevel.Warning && evt.MessageTemplate.Text == "db warning");
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
