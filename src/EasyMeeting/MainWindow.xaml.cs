using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using EasyMeeting.Models;
using EasyMeeting.Services;
using EasyMeeting.Services.Audio;
using EasyMeeting.Services.Recognition;
using EasyMeeting.Services.LLM;
using EasyMeeting.Services.Config;
using EasyMeeting.Services.Recording;
using EasyMeeting.ViewModels;
using EasyMeeting.UI.Views;
using EasyMeeting.Infrastructure;

namespace EasyMeeting;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ConfigService _configService;
    private readonly DispatcherTimer _durationTimer;
    private TimeSpan _recordingDuration;
    private RecordingOrchestrator? _orchestrator;
    private TrayIconManager? _trayIconManager;
    
    public MainWindow()
    {
        InitializeComponent();
        
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        _configService = new ConfigService();
        
        AudioSourceCombo.ItemsSource = Enum.GetValues<AudioSourceType>();
        AudioSourceCombo.SelectedItem = _configService.Current.AudioSource;
        
        EngineCombo.ItemsSource = Enum.GetValues<SpeechEngineType>();
        EngineCombo.SelectedItem = _configService.Current.SpeechEngine;
        
        _durationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _durationTimer.Tick += DurationTimer_Tick;
        
        OutputDirText.Text = _configService.Current.OutputDirectory;
        
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        ApplyUiLanguage();
    }
    
    private void ApplyUiLanguage()
    {
        bool isChinese = _configService.Current.UiLanguage == "zh";
        
        AudioSourceLabel.Text = isChinese ? "音频来源:" : "Audio Source:";
        SpeechEngineLabel.Text = isChinese ? "语音引擎:" : "Speech Engine:";
        DurationLabel.Text = isChinese ? "时长:" : "Duration:";
        LiveTranscriptionLabel.Text = isChinese ? "实时转录" : "Live Transcription";
        ClearButton.Content = isChinese ? "清空" : "Clear";
        HotkeyHint.Text = isChinese ? "Ctrl+Shift+Space: 切换预览" : "Ctrl+Shift+Space: Toggle Preview";
        
        StartButton.Content = isChinese ? "▶ 开始录制" : "▶ Start Recording";
        StopButton.Content = isChinese ? "⏹ 停止" : "⏹ Stop";
        SettingsButton.Content = isChinese ? "⚙️ 设置" : "⚙️ Settings";
        
        StatusText.Text = isChinese ? "就绪 - 点击开始录制" : "Ready - Click Start Recording to begin";
    }
    
    public void SetTrayIconManager(TrayIconManager trayIconManager)
    {
        _trayIconManager = trayIconManager;
        
        _trayIconManager.ShowRequested += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
        };
        
        _trayIconManager.SettingsRequested += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                var settingsWindow = new SettingsWindow { Owner = this };
                settingsWindow.ShowDialog();
                
                AudioSourceCombo.SelectedItem = _configService.Current.AudioSource;
                EngineCombo.SelectedItem = _configService.Current.SpeechEngine;
                OutputDirText.Text = _configService.Current.OutputDirectory;
                ApplyUiLanguage();
            });
        };
        
        _trayIconManager.StartRecordingRequested += (s, e) =>
        {
            Dispatcher.Invoke(() => StartButton_Click(this, new RoutedEventArgs()));
        };
        
        _trayIconManager.StopRecordingRequested += (s, e) =>
        {
            Dispatcher.Invoke(() => StopButton_Click(this, new RoutedEventArgs()));
        };
        
        _trayIconManager.RecordingModeChanged += (s, mode) =>
        {
            Dispatcher.Invoke(() =>
            {
                AudioSourceCombo.SelectedItem = mode;
                _configService.Current.AudioSource = mode;
                _configService.Save(_configService.Current);
            });
        };
        
        _trayIconManager.ExitRequested += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                _trayIconManager?.Dispose();
                System.Windows.Application.Current.Shutdown();
            });
        };
    }
    
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Transcript))
        {
            Dispatcher.BeginInvoke(() =>
            {
                TranscriptBox.ScrollToEnd();
            }, DispatcherPriority.Background);
        }
    }
    
    private void DurationTimer_Tick(object? sender, EventArgs e)
    {
        _recordingDuration = _recordingDuration.Add(TimeSpan.FromSeconds(1));
        DurationText.Text = _recordingDuration.ToString(@"hh\:mm\:ss");
    }
    
    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DebugService.Log($"Start recording: AudioSource={AudioSourceCombo.SelectedItem}, Engine={EngineCombo.SelectedItem}");
            
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            EngineCombo.IsEnabled = false;
            AudioSourceCombo.IsEnabled = false;
            
            _configService.Current.AudioSource = (AudioSourceType)AudioSourceCombo.SelectedItem;
            _configService.Current.SpeechEngine = (SpeechEngineType)EngineCombo.SelectedItem;
            _configService.Save(_configService.Current);
            
            IAudioCaptureService audioCapture;
            AudioSourceType selectedAudioSource = (AudioSourceType)AudioSourceCombo.SelectedItem;
            
            if (_configService.Current.SpeechEngine == SpeechEngineType.SAPI && selectedAudioSource == AudioSourceType.Both)
            {
                selectedAudioSource = AudioSourceType.Microphone;
                AudioSourceCombo.SelectedItem = selectedAudioSource;
            }
            
            audioCapture = selectedAudioSource switch
            {
                AudioSourceType.SystemAudio => new WasapiLoopbackCaptureService(),
                AudioSourceType.Microphone => new WasapiMicCaptureService(),
                _ => new DualAudioCaptureService()
            };
            
            ISpeechRecognitionService recognition = _configService.Current.SpeechEngine switch
            {
                SpeechEngineType.SAPI => new SapiRecognitionService(),
                _ => new WhisperRecognitionService()
            };
            
            _configService.Current.AudioSource = selectedAudioSource;
            _configService.Current.SpeechEngine = (SpeechEngineType)EngineCombo.SelectedItem;
            _configService.Save(_configService.Current);
            
            ILLmService llmService;
            var llmConfig = _configService.Current.Llm;
            if (llmConfig.Provider.ToLowerInvariant() == "claude")
            {
                llmService = new ClaudeLlmService(llmConfig);
            }
            else
            {
                llmService = new OpenAiLlmService(llmConfig);
            }
            
            _orchestrator = new RecordingOrchestrator(audioCapture, recognition, llmService, _configService);
            _orchestrator.OnTranscriptUpdate += (s, text) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _viewModel.Transcript = text;
                });
            };
            
            await _orchestrator.StartAsync();
            
            DebugService.Log("Recording started successfully - updating UI");
            
            _recordingDuration = TimeSpan.Zero;
            DurationText.Text = "00:00:00";
            DebugService.Log("Setting up timer and indicator");
            _durationTimer.Start();
            RecordingIndicator.Visibility = Visibility.Visible;
            _viewModel.StatusText = "Recording...";
            StatusText.Text = "Recording in progress...";
            
            DebugService.Log($"TrayIconManager is null: {_trayIconManager == null}");
            _trayIconManager?.StartAnimation();
            _trayIconManager?.SetRecordingState(true);
            DebugService.Log("UI update complete");
        }
        catch (Exception ex)
        {
            DebugService.LogError("Failed to start recording", ex);
            MessageBox.Show($"Failed to start recording: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
    }
    
    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DebugService.Log("Stop recording requested");
            
            _durationTimer.Stop();
            RecordingIndicator.Visibility = Visibility.Collapsed;
            _viewModel.StatusText = "Stopping...";
            StatusText.Text = "Processing and saving...";
            
            StopButton.IsEnabled = false;
            
            if (_orchestrator != null)
            {
                var (rawPath, summaryPath) = await _orchestrator.StopAsync();
                _orchestrator.Dispose();
                _orchestrator = null;
                
                DebugService.Log($"Recording saved: {rawPath}, {summaryPath}");
                
                _viewModel.StatusText = "Completed";
                StatusText.Text = $"Saved: {Path.GetFileName(rawPath)} and {Path.GetFileName(summaryPath)}";
                
                MessageBox.Show(
                    $"Recording saved:\n\nRaw: {rawPath}\n\nSummary: {summaryPath}",
                    "Recording Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            EngineCombo.IsEnabled = true;
            AudioSourceCombo.IsEnabled = true;
            
            _trayIconManager?.StopAnimation();
            _trayIconManager?.SetRecordingState(false);
        }
        catch (Exception ex)
        {
            DebugService.LogError("Failed to stop recording", ex);
            MessageBox.Show($"Failed to stop recording: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StartButton.IsEnabled = true;
        }
    }
    
    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Transcript = string.Empty;
    }
    
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
        
        AudioSourceCombo.SelectedItem = _configService.Current.AudioSource;
        EngineCombo.SelectedItem = _configService.Current.SpeechEngine;
        OutputDirText.Text = _configService.Current.OutputDirectory;
        ApplyUiLanguage();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _durationTimer.Stop();
        _orchestrator?.Dispose();
        base.OnClosed(e);
    }
}
