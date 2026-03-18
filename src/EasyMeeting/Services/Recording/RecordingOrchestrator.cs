using System.IO;
using EasyMeeting.Services.Audio;
using EasyMeeting.Services.Recognition;
using EasyMeeting.Services.LLM;
using EasyMeeting.Services.Config;
using EasyMeeting.Models;

namespace EasyMeeting.Services.Recording;

public class RecordingOrchestrator : IDisposable
{
    private readonly IAudioCaptureService _audioCapture;
    private readonly ISpeechRecognitionService _recognition;
    private readonly ILLmService _llmService;
    private readonly ConfigService _configService;
    private readonly List<string> _transcriptSegments = new();
    private CancellationTokenSource? _cts;
    private string? _outputDirectory;
    
    public bool IsRecording { get; private set; }
    public event EventHandler<string>? OnTranscriptUpdate;
    
    public RecordingOrchestrator(
        IAudioCaptureService audioCapture,
        ISpeechRecognitionService recognition,
        ILLmService llmService,
        ConfigService configService)
    {
        _audioCapture = audioCapture;
        _recognition = recognition;
        _llmService = llmService;
        _configService = configService;
    }
    
    public async Task StartAsync()
    {
        var config = _configService.Current;
        _outputDirectory = config.OutputDirectory;
        Directory.CreateDirectory(_outputDirectory);
        
        _cts = new CancellationTokenSource(TimeSpan.FromMinutes(config.MaxRecordingMinutes));
        
        string? modelPath = null;
        if (config.SpeechEngine == SpeechEngineType.Whisper)
        {
            modelPath = config.WhisperModelPath;
        }
        
        await _recognition.InitializeAsync(modelPath);
        
        _audioCapture.OnAudioAvailable += async (s, sample) =>
        {
            try
            {
                await foreach (var result in _recognition.RecognizeAsync(sample, _cts!.Token))
                {
                    _transcriptSegments.Add(result.Text);
                    OnTranscriptUpdate?.Invoke(this, string.Join(" ", _transcriptSegments));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }
        };
        
        await _audioCapture.StartAsync(_cts.Token);
        IsRecording = true;
    }
    
    public async Task<(string rawPath, string summaryPath)> StopAsync()
    {
        _cts?.Cancel();
        await _audioCapture.StopAsync();
        IsRecording = false;
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
        var rawPath = Path.Combine(_outputDirectory!, $"{timestamp}_raw.md");
        var summaryPath = Path.Combine(_outputDirectory!, $"{timestamp}_summary.md");
        
        var fullTranscript = string.Join(" ", _transcriptSegments);
        await File.WriteAllTextAsync(rawPath, $"# Transcription\n\n{fullTranscript}");
        
        var config = _configService.Current;
        try
        {
            var summary = await _llmService.SummarizeAsync(fullTranscript, config.SummaryLanguage, CancellationToken.None);
            await File.WriteAllTextAsync(summaryPath, $"# Summary\n\n{summary}");
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync(summaryPath, $"# Summary\n\n[Error generating summary: {ex.Message}]");
        }
        
        return (rawPath, summaryPath);
    }
    
    public void Dispose()
    {
        _cts?.Dispose();
    }
}
