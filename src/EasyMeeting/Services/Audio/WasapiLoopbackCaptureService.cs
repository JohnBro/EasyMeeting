using NAudio.Wave;
using EasyMeeting.Models;

namespace EasyMeeting.Services.Audio;

public class WasapiLoopbackCaptureService : IAudioCaptureService
{
    private WasapiLoopbackCapture? _waveIn;
    
    public event EventHandler<AudioSample>? OnAudioAvailable;
    public bool IsRunning { get; private set; }
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            _waveIn = new WasapiLoopbackCapture();
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
            IsRunning = true;
        }, ct);
    }
    
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var buffer = new float[e.BytesRecorded / 2];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;
        }
        OnAudioAvailable?.Invoke(this, new AudioSample(buffer, _waveIn!.WaveFormat.SampleRate, _waveIn.WaveFormat.Channels));
    }
    
    public Task StopAsync()
    {
        _waveIn?.StopRecording();
        IsRunning = false;
        return Task.CompletedTask;
    }
    
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _waveIn?.Dispose();
        _waveIn = null;
    }
}
