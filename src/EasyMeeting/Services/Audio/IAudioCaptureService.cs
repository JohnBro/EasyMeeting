using EasyMeeting.Models;

namespace EasyMeeting.Services.Audio;

public interface IAudioCaptureService : IAsyncDisposable
{
    event EventHandler<AudioSample>? OnAudioAvailable;
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    bool IsRunning { get; }
}
