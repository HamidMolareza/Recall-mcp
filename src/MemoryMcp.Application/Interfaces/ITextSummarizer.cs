namespace MemoryMcp.Application.Interfaces;

public interface ITextSummarizer
{
    Task<string> SummarizeAsync(string text, CancellationToken ct = default);
}
