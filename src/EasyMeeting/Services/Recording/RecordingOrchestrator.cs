using System.IO;
using System.Text;
using EasyMeeting;
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
    private readonly List<(TimeSpan timestamp, string text)> _transcriptEntries = new();
    private CancellationTokenSource? _cts;
    private string? _outputDirectory;
    
    private List<float> _audioBuffer = new();
    private readonly int _sampleRate = 16000;
    private double _minBufferSeconds = 3.0;
    private long _cumulativeSamples = 0;
    private readonly object _bufferLock = new();
    
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
        
        _minBufferSeconds = config.AudioBufferSeconds > 0 ? config.AudioBufferSeconds : 3.0;
        _cumulativeSamples = 0;
        
        _cts = new CancellationTokenSource(TimeSpan.FromMinutes(config.MaxRecordingMinutes));
        
        string? modelPath = null;
        if (config.SpeechEngine == SpeechEngineType.Whisper)
        {
            modelPath = config.WhisperModelPath;
            
            if (string.IsNullOrEmpty(modelPath))
            {
                var modelsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".easymeeting", "models");
                var modelFileName = config.WhisperModelSize switch
                {
                    WhisperModelSize.Tiny => "ggml-tiny.bin",
                    WhisperModelSize.Base => "ggml-base.bin",
                    WhisperModelSize.Small => "ggml-small.bin",
                    WhisperModelSize.Medium => "ggml-medium.bin",
                    WhisperModelSize.LargeV3Turbo => "ggml-large-v3-turbo.bin",
                    _ => "ggml-base.bin"
                };
                var defaultPath = Path.Combine(modelsDir, modelFileName);
                if (File.Exists(defaultPath))
                    modelPath = defaultPath;
            }
        }
        
        await _recognition.InitializeAsync(modelPath);
        
        if (_recognition is WhisperRecognitionService whisperService)
        {
            var whisperLang = config.SummaryLanguage switch
            {
                "zh" => "auto",
                "ja" => "ja",
                _ => "auto"
            };
            whisperService.SetLanguage(whisperLang);
            DebugService.Log($"Whisper language set to: {whisperLang} (SummaryLanguage: {config.SummaryLanguage})");
        }
        
        _audioCapture.OnAudioAvailable += async (s, sample) =>
        {
            try
            {
                ProcessAudioSample(sample);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                DebugService.LogError("Audio processing error", ex);
            }
        };
        
        await _audioCapture.StartAsync(_cts.Token);
        IsRecording = true;
    }
    
    private async void ProcessAudioSample(AudioSample sample)
    {
        int minSamples = (int)(_sampleRate * _minBufferSeconds);
        
        lock (_bufferLock)
        {
            _audioBuffer.AddRange(sample.Data);
        }
        
        while (true)
        {
            List<float> bufferToProcess;
            lock (_bufferLock)
            {
                if (_audioBuffer.Count < minSamples)
                    break;
                bufferToProcess = new List<float>(_audioBuffer.Take(minSamples));
                _audioBuffer.RemoveRange(0, minSamples);
            }
            
            var audioSample = new AudioSample(bufferToProcess.ToArray(), _sampleRate, 1);
            var segmentTimestamp = TimeSpan.FromSeconds((double)_cumulativeSamples / _sampleRate);
            
            lock (_bufferLock)
            {
                _cumulativeSamples += bufferToProcess.Count;
            }
            
            try
            {
                await foreach (var result in _recognition.RecognizeAsync(audioSample, _cts!.Token))
                {
                    if (!string.IsNullOrWhiteSpace(result.Text))
                    {
                        var entryTimestamp = segmentTimestamp + result.Duration;
                        _transcriptEntries.Add((entryTimestamp, result.Text));
                        
                        var transcript = FormatTranscriptWithTimestamps();
                        OnTranscriptUpdate?.Invoke(this, transcript);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugService.LogError("Whisper processing error", ex);
            }
        }
    }
    
    private string FormatTranscriptWithTimestamps()
    {
        var sb = new StringBuilder();
        foreach (var (timestamp, text) in _transcriptEntries)
        {
            sb.AppendLine($"[{timestamp:mm\\:ss\\.ff}] {text}");
        }
        return sb.ToString();
    }
    
    public async Task<(string rawPath, string summaryPath)> StopAsync()
    {
        _cts?.Cancel();
        await _audioCapture.StopAsync();
        IsRecording = false;
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
        var rawPath = Path.Combine(_outputDirectory!, $"{timestamp}_raw.md");
        var summaryPath = Path.Combine(_outputDirectory!, $"{timestamp}_summary.md");
        
        var fullTranscript = string.Join(" ", _transcriptEntries.Select(e => e.text));
        var transcriptWithTimestamps = FormatTranscriptWithTimestamps();
        
        await File.WriteAllTextAsync(rawPath, $"# Transcription\n\n{transcriptWithTimestamps}");
        
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
