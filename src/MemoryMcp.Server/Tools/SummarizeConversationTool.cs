using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Application.Services;
using MemoryMcp.Domain.Enums;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MemoryMcp.Server.Tools;

[McpServerToolType]
public sealed class SummarizeConversationTool
{
    private readonly ITextSummarizer _summarizer;
    private readonly MemoryService _memoryService;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<SummarizeConversationTool> _logger;

    public SummarizeConversationTool(
        ITextSummarizer summarizer,
        MemoryService memoryService,
        IOperationNoticeSink operationNoticeSink,
        ILogger<SummarizeConversationTool> logger)
    {
        _summarizer = summarizer;
        _memoryService = memoryService;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    [McpServerTool(Name = "summarize_conversation"), Description("Summarizes conversation text and can optionally store the summary as memory.")]
    public async Task<object> SummarizeConversationAsync(
        string conversation,
        bool storeSummary = false,
        string? memoryNamespace = null,
        MemoryType summaryType = MemoryType.Context,
        string[]? tags = null,
        CancellationToken ct = default)
    {
        return await ToolExecutionHelper.ExecuteAsync("summarize_conversation", _logger, _operationNoticeSink, async innerCt =>
        {
            var summary = await _summarizer.SummarizeAsync(conversation, innerCt);

            if (!storeSummary)
            {
                return new { summary };
            }

            if (string.IsNullOrWhiteSpace(memoryNamespace))
            {
                throw new ArgumentException("A namespace is required when storing the generated summary.", nameof(memoryNamespace));
            }

            var memory = await _memoryService.StoreAsync(new StoreMemoryRequest
            {
                Content = summary,
                Type = summaryType,
                Namespace = memoryNamespace,
                Tags = tags
            }, innerCt);

            _logger.LogInformation("Stored summarized conversation as memory {MemoryId}", memory.Id);
            return new
            {
                summary,
                memory
            };
        }, ct);
    }
}
