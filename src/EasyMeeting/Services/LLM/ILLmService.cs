namespace EasyMeeting.Services.LLM;

public interface ILLmService
{
    IAsyncEnumerable<string> SummarizeStreamingAsync(string text, string language, CancellationToken ct = default);
    Task<string> SummarizeAsync(string text, string language, CancellationToken ct = default);
}
