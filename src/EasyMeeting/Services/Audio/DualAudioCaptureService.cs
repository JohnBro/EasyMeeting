using EasyMeeting.Models;

namespace EasyMeeting.Services.Audio;

public class DualAudioCaptureService : IAudioCaptureService
{
    private WasapiLoopbackCaptureService? _loopback;
    private WasapiMicCaptureService? _mic;
    private AudioMixer _mixer = new();
    
    public event EventHandler<AudioSample>? OnAudioAvailable;
    public bool IsRunning { get; private set; }
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        _loopback = new WasapiLoopbackCaptureService();
        _mic = new WasapiMicCaptureService();
        
        AudioSample? loopbackSample = null;
        AudioSample? micSample = null;
        object lockObj = new();
        
        _loopback.OnAudioAvailable += (s, sample) =>
        {
            lock (lockObj) { loopbackSample = sample; }
            TryMixAndPublish(loopbackSample, micSample);
        };
        
        _mic.OnAudioAvailable += (s, sample) =>
        {
            lock (lockObj) { micSample = sample; }
            TryMixAndPublish(loopbackSample, micSample);
        };
        
        await Task.WhenAll(
            _loopback.StartAsync(ct),
            _mic.StartAsync(ct)
        );
        
        IsRunning = true;
    }
    
    private void TryMixAndPublish(AudioSample? loopback, AudioSample? mic)
    {
        if (loopback == null || mic == null) return;
        
        try
        {
            var mixed = _mixer.Mix(loopback, mic);
            OnAudioAvailable?.Invoke(this, mixed);
        }
        catch
        {
        }
    }
    
    public async Task StopAsync()
    {
        if (_loopback != null) await _loopback.StopAsync();
        if (_mic != null) await _mic.StopAsync();
        IsRunning = false;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_loopback != null) await _loopback.DisposeAsync();
        if (_mic != null) await _mic.DisposeAsync();
    }
}

public class AudioMixer
{
    public AudioSample Mix(AudioSample left, AudioSample right)
    {
        if (left.SampleRate != right.SampleRate)
            throw new InvalidOperationException("Sample rates must match");
        
        var maxLength = Math.Max(left.Data.Length, right.Data.Length);
        var result = new float[maxLength];
        
        for (int i = 0; i < maxLength; i++)
        {
            var l = i < left.Data.Length ? left.Data[i] : 0;
            var r = i < right.Data.Length ? right.Data[i] : 0;
            result[i] = (l + r) / 2;
        }
        
        return new AudioSample(result, left.SampleRate, 1);
    }
}
