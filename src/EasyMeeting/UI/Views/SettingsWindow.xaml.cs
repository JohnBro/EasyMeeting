using System.IO;
using System.Windows;
using System.Windows.Controls;
using EasyMeeting.Models;
using EasyMeeting.Services.Config;
using EasyMeeting.Services.Download;
using Microsoft.Win32;

namespace EasyMeeting.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly AppConfig _config;
    private WhisperModelDownloadService? _downloadService;
    private CancellationTokenSource? _downloadCts;
    
    public SettingsWindow()
    {
        InitializeComponent();
        
        _configService = new ConfigService();
        _config = _configService.Current;
        
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        SpeechEngineCombo.ItemsSource = Enum.GetValues<SpeechEngineType>();
        SpeechEngineCombo.SelectedItem = _config.SpeechEngine;
        
        AudioSourceCombo.ItemsSource = Enum.GetValues<AudioSourceType>();
        AudioSourceCombo.SelectedItem = _config.AudioSource;
        
        MaxRecordingText.Text = _config.MaxRecordingMinutes.ToString();
        OutputDirText.Text = _config.OutputDirectory;
        SummaryLangCombo.SelectedIndex = _config.SummaryLanguage switch
        {
            "zh" => 1,
            "ja" => 2,
            _ => 0
        };
        
        UiLangCombo.SelectedIndex = _config.UiLanguage == "zh" ? 1 : 0;
        
        WhisperModelCombo.ItemsSource = Enum.GetValues<WhisperModelSize>();
        WhisperModelCombo.SelectedItem = _config.WhisperModelSize;
        UpdateWhisperModelStatus();
        
        AudioBufferCombo.ItemsSource = new double[] { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0 };
        AudioBufferCombo.SelectedItem = _config.AudioBufferSeconds > 0 ? _config.AudioBufferSeconds : 3.0;
        
        LlmProviderCombo.SelectedIndex = _config.Llm.Provider.ToLowerInvariant() == "claude" ? 1 : 0;
        ApiKeyText.Password = _config.Llm.ApiKey;
        BaseUrlText.Text = _config.Llm.BaseUrl;
        ModelText.Text = _config.Llm.Model;
    }
    
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Output Directory"
        };
        
        if (dialog.ShowDialog() == true)
        {
            OutputDirText.Text = dialog.FolderName;
        }
    }
    
    private void UpdateWhisperModelStatus()
    {
        var modelsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".easymeeting", "models");
        
        var selectedSize = WhisperModelCombo.SelectedItem is WhisperModelSize size ? size : WhisperModelSize.Base;
        var expectedFileName = $"ggml-{GetModelFileSuffix(selectedSize)}.bin";
        var expectedPath = Path.Combine(modelsDir, expectedFileName);
        
        if (File.Exists(expectedPath))
        {
            WhisperModelPathText.Text = expectedPath;
            DownloadStatusText.Text = "Downloaded";
            DownloadStatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        else if (!string.IsNullOrEmpty(_config.WhisperModelPath) && File.Exists(_config.WhisperModelPath))
        {
            WhisperModelPathText.Text = _config.WhisperModelPath;
            DownloadStatusText.Text = "Custom path set";
            DownloadStatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }
        else
        {
            WhisperModelPathText.Text = "(not downloaded)";
            DownloadStatusText.Text = "Not downloaded - click Download to get started";
            DownloadStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        }
    }
    
    private static string GetModelFileSuffix(WhisperModelSize size) => size switch
    {
        WhisperModelSize.Tiny => "tiny",
        WhisperModelSize.Base => "base",
        WhisperModelSize.Small => "small",
        WhisperModelSize.Medium => "medium",
        WhisperModelSize.LargeV3Turbo => "large-v3-turbo",
        _ => "base"
    };
    
    private async void DownloadModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_downloadService?.IsDownloading == true)
        {
            _downloadCts?.Cancel();
            return;
        }
        
        if (WhisperModelCombo.SelectedItem is not WhisperModelSize selectedSize)
        {
            return;
        }
        
        var modelsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".easymeeting", "models");
        Directory.CreateDirectory(modelsDir);
        
        _downloadService = new WhisperModelDownloadService(modelsDir);
        _downloadCts = new CancellationTokenSource();
        
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.Value = 0;
        DownloadModelButton.Content = "Cancel";
        DownloadStatusText.Text = $"Downloading {selectedSize} model...";
        DownloadStatusText.Foreground = System.Windows.Media.Brushes.Blue;
        
        try
        {
            _downloadService.OnProgressChanged += (s, progress) =>
            {
                Dispatcher.Invoke(() =>
                {
                    DownloadProgress.Value = progress * 100;
                    DownloadStatusText.Text = $"Downloading {selectedSize} model... {progress * 100:F0}%";
                });
            };
            
            var path = await _downloadService.DownloadModelAsync(selectedSize, _downloadCts.Token);
            _config.WhisperModelPath = path;
            _config.WhisperModelSize = selectedSize;
            
            DownloadStatusText.Text = "Download complete!";
            DownloadStatusText.Foreground = System.Windows.Media.Brushes.Green;
            UpdateWhisperModelStatus();
        }
        catch (OperationCanceledException)
        {
            DownloadStatusText.Text = "Download cancelled.";
            DownloadStatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }
        catch (Exception ex)
        {
            DownloadStatusText.Text = $"Error: {ex.Message}";
            DownloadStatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
        finally
        {
            DownloadProgress.Visibility = Visibility.Collapsed;
            DownloadModelButton.Content = "Download";
            _downloadService = null;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }
    
    private void LlmProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BaseUrlText == null) return;
        
        bool isOpenAi = LlmProviderCombo.SelectedIndex == 0;
        BaseUrlText.Text = isOpenAi 
            ? "https://api.openai.com/v1"
            : "https://api.anthropic.com/v1";
        ModelText.Text = isOpenAi ? "gpt-4o-mini" : "claude-sonnet-4-20250514";
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _config.SpeechEngine = (SpeechEngineType)SpeechEngineCombo.SelectedItem;
        _config.AudioSource = (AudioSourceType)AudioSourceCombo.SelectedItem;
        
        if (int.TryParse(MaxRecordingText.Text, out int maxMins) && maxMins > 0)
            _config.MaxRecordingMinutes = maxMins;
        
        _config.OutputDirectory = OutputDirText.Text;
        
        _config.SummaryLanguage = ((ComboBoxItem)SummaryLangCombo.SelectedItem)?.Tag?.ToString() ?? "en";
        _config.UiLanguage = ((ComboBoxItem)UiLangCombo.SelectedItem)?.Tag?.ToString() ?? "en";
        
        _config.Llm.Provider = ((ComboBoxItem)LlmProviderCombo.SelectedItem)?.Tag?.ToString() ?? "openai";
        _config.Llm.ApiKey = ApiKeyText.Password;
        _config.Llm.BaseUrl = BaseUrlText.Text;
        _config.Llm.Model = ModelText.Text;
        
        _config.WhisperModelSize = (WhisperModelSize)(WhisperModelCombo.SelectedItem ?? WhisperModelSize.Base);
        _config.AudioBufferSeconds = (double)(AudioBufferCombo.SelectedItem ?? 3.0);
        
        _configService.Save(_config);
        
        DialogResult = true;
        Close();
    }
}
