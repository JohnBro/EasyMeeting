using System.IO;
using System.Net.Http;
using EasyMeeting.Models;

namespace EasyMeeting.Services.Download;

public class WhisperModelDownloadService : IWhisperModelDownloadService
{
    private readonly string _defaultSaveDir;
    private readonly HttpClient _httpClient;
    private bool _isDownloading;
    
    private static readonly Dictionary<WhisperModelSize, string> ModelFileNames = new()
    {
        { WhisperModelSize.Tiny, "ggml-tiny.bin" },
        { WhisperModelSize.Base, "ggml-base.bin" },
        { WhisperModelSize.Small, "ggml-small.bin" },
        { WhisperModelSize.Medium, "ggml-medium.bin" },
        { WhisperModelSize.LargeV3Turbo, "ggml-large-v3-turbo.bin" }
    };

    public event EventHandler<double>? OnProgressChanged;
    public bool IsDownloading => _isDownloading;

    public WhisperModelDownloadService(string? saveDirectory = null)
    {
        _defaultSaveDir = saveDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".easymeeting", "models");
        
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    public async Task<string> DownloadModelAsync(WhisperModelSize size, CancellationToken ct = default)
    {
        if (!ModelFileNames.TryGetValue(size, out var fileName))
        {
            throw new ArgumentException($"Unknown model size: {size}");
        }

        var savePath = Path.Combine(_defaultSaveDir, fileName);
        
        Directory.CreateDirectory(_defaultSaveDir);

        if (File.Exists(savePath))
        {
            var fileInfo = new FileInfo(savePath);
            if (fileInfo.Length > 0)
            {
                return savePath;
            }
            File.Delete(savePath);
        }

        var url = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/{fileName}";
        
        _isDownloading = true;
        
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(
                request, 
                HttpCompletionOption.ResponseHeadersRead, 
                ct);
            
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;
            
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(
                savePath, 
                FileMode.Create, 
                FileAccess.Write, 
                FileShare.None, 
                8192, 
                true);
            
            var totalRead = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;
            
            while (isMoreToRead && !ct.IsCancellationRequested)
            {
                var read = await contentStream.ReadAsync(buffer, ct);
                if (read == 0)
                {
                    isMoreToRead = false;
                }
                else
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                    totalRead += read;
                    
                    if (canReportProgress)
                    {
                        var progress = (double)totalRead / totalBytes;
                        OnProgressChanged?.Invoke(this, progress);
                    }
                }
            }
            
            if (ct.IsCancellationRequested)
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
                ct.ThrowIfCancellationRequested();
            }
            
            return savePath;
        }
        finally
        {
            _isDownloading = false;
        }
    }
}
