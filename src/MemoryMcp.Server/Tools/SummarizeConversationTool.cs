using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Server.Tools;

public sealed class SummarizeConversationTool
{
    private readonly ITextSummarizer _summarizer;
    private readonly ILogger<SummarizeConversationTool> _logger;

    public SummarizeConversationTool(ITextSummarizer summarizer, ILogger<SummarizeConversationTool> logger)
    {
        _summarizer = summarizer;
        _logger = logger;
    }
}
