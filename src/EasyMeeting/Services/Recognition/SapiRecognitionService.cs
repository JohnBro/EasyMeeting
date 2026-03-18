using System.Speech.Recognition;
using System.Threading.Channels;
using EasyMeeting.Models;
using RecognitionResult = EasyMeeting.Models.RecognitionResult;

namespace EasyMeeting.Services.Recognition;

public class SapiRecognitionService : ISpeechRecognitionService
{
    private SpeechRecognitionEngine? _recognizer;
    private readonly Channel<RecognitionResult> _resultChannel = Channel.CreateUnbounded<RecognitionResult>();
    private bool _isProcessing;
    private bool _isInitialized;
    private readonly object _lock = new();
    
    public bool IsInitialized => _isInitialized;
    public event EventHandler<Models.RecognitionResult>? OnPartialResult;
    
    public Task InitializeAsync(string? modelPath = null)
    {
        try
        {
            _recognizer = new SpeechRecognitionEngine();
            
            var dictationGrammar = new DictationGrammar();
            dictationGrammar.Name = "DictationGrammar";
            dictationGrammar.Enabled = true;
            _recognizer.LoadGrammar(dictationGrammar);
            
            try
            {
                _recognizer.SetInputToDefaultAudioDevice();
            }
            catch (InvalidOperationException)
            {
            }
            
            _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            _recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
            _recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
            
            _isInitialized = true;
        }
        catch (Exception)
        {
            _isInitialized = false;
            throw;
        }
        
        return Task.CompletedTask;
    }
    
    private void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result != null && e.Result.Confidence > 0.3)
        {
            var result = new RecognitionResult(
                e.Result.Text,
                TimeSpan.FromSeconds(1),
                true);
            
            _resultChannel.Writer.TryWrite(result);
            OnPartialResult?.Invoke(this, result);
        }
    }
    
    private void Recognizer_SpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e)
    {
        if (e.Result != null)
        {
            var result = new RecognitionResult(
                e.Result.Text,
                TimeSpan.FromSeconds(0.5),
                false);
            
            _resultChannel.Writer.TryWrite(result);
            OnPartialResult?.Invoke(this, result);
        }
    }
    
    private void Recognizer_RecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        lock (_lock)
        {
            if (!_isProcessing) return;
            
            if (e.Error != null)
            {
                return;
            }
        }
        
        try
        {
            _recognizer?.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch
        {
        }
    }
    
    public async IAsyncEnumerable<Models.RecognitionResult> RecognizeAsync(AudioSample sample, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_isInitialized || _recognizer == null)
            throw new InvalidOperationException("Not initialized. Call InitializeAsync first.");
        
        lock (_lock)
        {
            if (_isProcessing) yield break;
            _isProcessing = true;
        }
        
        try
        {
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch (Exception)
        {
            lock (_lock) { _isProcessing = false; }
            yield break;
        }
        
        while (!ct.IsCancellationRequested)
        {
            RecognitionResult? result = null;
            
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
                
                var readTask = _resultChannel.Reader.ReadAsync(linkedCts.Token).AsTask();
                var timeoutTask = Task.Delay(100, linkedCts.Token);
                
                var completedTask = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);
                
                if (completedTask == readTask && !readTask.IsCanceled && !readTask.IsFaulted)
                {
                    result = readTask.Result;
                }
            }
            catch (OperationCanceledException)
            {
                if (ct.IsCancellationRequested) break;
                continue;
            }
            catch (Exception)
            {
                break;
            }
            
            if (result != null)
            {
                yield return result;
            }
        }
        
        lock (_lock) { _isProcessing = false; }
    }
    
    public async ValueTask DisposeAsync()
    {
        lock (_lock) { _isProcessing = false; }
        
        try
        {
            _resultChannel.Writer.Complete();
        }
        catch
        {
        }
        
        if (_recognizer != null)
        {
            try
            {
                _recognizer.RecognizeAsyncCancel();
            }
            catch
            {
            }
            
            _recognizer.SpeechRecognized -= Recognizer_SpeechRecognized;
            _recognizer.RecognizeCompleted -= Recognizer_RecognizeCompleted;
            _recognizer.SpeechHypothesized -= Recognizer_SpeechHypothesized;
            _recognizer.Dispose();
            _recognizer = null;
        }
        
        _isInitialized = false;
        await Task.CompletedTask;
    }
}
