using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace MemoryMcp.Infrastructure.Text;

public sealed class OpenAiResponseSummarizer : ITextSummarizer
{
    private readonly ResponsesClient _client;
    private readonly AiSummarizerOptions _options;
    private readonly ILogger<OpenAiResponseSummarizer> _logger;

    public OpenAiResponseSummarizer(
        AiSummarizerOptions options,
        ILogger<OpenAiResponseSummarizer> logger)
    {
        _options = options;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("AI API key is required for the OpenAI-compatible responses summarizer.");
        }

        _client = new ResponsesClient(
            credential: new ApiKeyCredential(options.ApiKey),
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(NormalizeBaseUrl(options.BaseUrl))
            });
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var prompt = $"""
            Summarize the following conversation into concise durable memory notes.
            Focus on facts, preferences, instructions, commitments, and relevant context.
            Keep it short and structured in plain text.

            Conversation:
            {text}
            """;

        try
        {
            ClientResult<ResponseResult> response = await _client.CreateResponseAsync(
                _options.Model,
                prompt,
                cancellationToken: ct);

            var output = response.Value.GetOutputText();
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException("The AI summarizer returned no text.");
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI-compatible summarization failed. Falling back to the simple local summarizer.");
            return await new SimpleTextSummarizer().SummarizeAsync(text, ct);
        }
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        return baseUrl.TrimEnd('/') + "/";
    }
}
