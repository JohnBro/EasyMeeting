using EasyMeeting.Models;

namespace EasyMeeting.Services.Download;

public interface IWhisperModelDownloadService
{
    event EventHandler<double>? OnProgressChanged;
    Task<string> DownloadModelAsync(WhisperModelSize size, CancellationToken ct = default);
    bool IsDownloading { get; }
}
