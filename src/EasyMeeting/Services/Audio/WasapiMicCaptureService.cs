using NAudio.Wave;
using EasyMeeting.Models;

namespace EasyMeeting.Services.Audio;

public class WasapiMicCaptureService : IAudioCaptureService
{
    private WaveInEvent? _waveIn;
    
    public event EventHandler<AudioSample>? OnAudioAvailable;
    public bool IsRunning { get; private set; }
    
    public Task StartAsync(CancellationToken ct = default)
    {
        _waveIn = new WaveInEvent
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(16000, 1)
        };
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.StartRecording();
        IsRunning = true;
        return Task.CompletedTask;
    }
    
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var buffer = new float[e.BytesRecorded / 2];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;
        }
        OnAudioAvailable?.Invoke(this, new AudioSample(buffer, 16000, 1));
    }
    
    public Task StopAsync()
    {
        _waveIn?.StopRecording();
        IsRunning = false;
        return Task.CompletedTask;
    }
    
    public ValueTask DisposeAsync()
    {
        _waveIn?.Dispose();
        _waveIn = null;
        return ValueTask.CompletedTask;
    }
}
