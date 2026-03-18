using EasyMeeting.Models;

namespace EasyMeeting.Services.Recognition;

public interface ISpeechRecognitionService : IAsyncDisposable
{
    event EventHandler<RecognitionResult>? OnPartialResult;
    Task InitializeAsync(string? modelPath = null);
    IAsyncEnumerable<RecognitionResult> RecognizeAsync(AudioSample sample, CancellationToken ct = default);
}
