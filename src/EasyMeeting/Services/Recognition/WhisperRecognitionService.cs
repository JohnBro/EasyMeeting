using System.IO;
using Whisper.net;
using EasyMeeting.Models;

namespace EasyMeeting.Services.Recognition;

public class WhisperRecognitionService : ISpeechRecognitionService
{
    private WhisperFactory? _factory;
    private string _language = "auto";
    
    public bool IsInitialized => _factory != null;
    public event EventHandler<Models.RecognitionResult>? OnPartialResult;
    
    public void SetLanguage(string language)
    {
        _language = string.IsNullOrEmpty(language) || language == "auto" ? "auto" : language;
    }
    
    public Task InitializeAsync(string? modelPath = null)
    {
        if (string.IsNullOrEmpty(modelPath))
        {
            throw new InvalidOperationException("Whisper model path is required. Please set WhisperModelPath in config.json or download a model.");
        }
        
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Whisper model not found at: {modelPath}");
        }
        
        return Task.Run(() =>
        {
            DebugService.Log($"Initializing Whisper from: {modelPath}");
            
            _factory = WhisperFactory.FromPath(modelPath);
            DebugService.Log($"Whisper initialized with language: {_language}");
        });
    }
    
    public async IAsyncEnumerable<Models.RecognitionResult> RecognizeAsync(AudioSample sample, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_factory == null)
            throw new InvalidOperationException("Not initialized. Call InitializeAsync first.");
        
        if (sample.Data == null || sample.Data.Length == 0)
        {
            DebugService.Log("Warning: Empty audio sample received");
            yield break;
        }
        
        DebugService.Log($"Processing audio sample: {sample.Data.Length} samples at {sample.SampleRate}Hz with lang: {_language}");
        
        using var processor = _factory.CreateBuilder()
            .WithLanguage(_language)
            .Build();
        
        var samples = sample.Data;
        
        int resultCount = 0;
        await foreach (var result in processor.ProcessAsync(samples, ct))
        {
            var text = result.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text) || text.Contains("[BLANK_AUDIO]") || text.Contains("[Music]"))
            {
                continue;
            }
            resultCount++;
            yield return new Models.RecognitionResult(
                text,
                result.Start + result.End,
                (result.End - result.Start).TotalSeconds < 1.0);
        }
        
        DebugService.Log($"Whisper processed {resultCount} transcription segments");
    }
    
    public async ValueTask DisposeAsync()
    {
        _factory = null;
        await Task.CompletedTask;
    }
}
