using Whisper.net;
using EasyMeeting.Models;

namespace EasyMeeting.Services.Recognition;

public class WhisperRecognitionService : ISpeechRecognitionService
{
    private WhisperFactory? _factory;
    
    public bool IsInitialized => _factory != null;
    public event EventHandler<Models.RecognitionResult>? OnPartialResult;
    
    public Task InitializeAsync(string? modelPath = null)
    {
        if (string.IsNullOrEmpty(modelPath))
        {
            throw new InvalidOperationException("Whisper model path is required. Please set WhisperModelPath in config.json or download a model.");
        }
        
        return Task.Run(() =>
        {
            _factory = WhisperFactory.FromPath(modelPath);
        });
    }
    
    public async IAsyncEnumerable<Models.RecognitionResult> RecognizeAsync(AudioSample sample, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_factory == null)
            throw new InvalidOperationException("Not initialized. Call InitializeAsync first.");
        
        using var processor = _factory.CreateBuilder()
            .WithLanguage("auto")
            .Build();
        
        var samples = sample.Data;
        
        await foreach (var result in processor.ProcessAsync(samples, ct))
        {
            yield return new Models.RecognitionResult(
                result.Text,
                result.Start + result.End,
                (result.End - result.Start).TotalSeconds < 1.0);
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        _factory = null;
        await Task.CompletedTask;
    }
}
