using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;
using EasyMeeting.Models;

namespace EasyMeeting.Infrastructure;

public class TrayIconManager : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly Window _mainWindow;
    private System.Windows.Threading.DispatcherTimer? _animationTimer;
    private int _animationFrame;
    private bool _isAnimating;
    
    private System.Windows.Controls.MenuItem? _startRecordingItem;
    private System.Windows.Controls.MenuItem? _stopRecordingItem;
    private System.Windows.Controls.MenuItem? _recordingModeItem;
    
    public event EventHandler? ShowRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? StartRecordingRequested;
    public event EventHandler? StopRecordingRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler<AudioSourceType>? RecordingModeChanged;
    
    private AudioSourceType _currentMode = AudioSourceType.Both;
    public AudioSourceType CurrentMode
    {
        get => _currentMode;
        set
        {
            _currentMode = value;
            UpdateModeMenuText();
        }
    }
    
    private bool _isRecording;
    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            _isRecording = value;
            UpdateMenuStates();
        }
    }
    
    public void SetRecordingState(bool recording)
    {
        if (recording)
        {
            StartAnimation();
        }
        else
        {
            StopAnimation();
        }
        IsRecording = recording;
    }
    
    public TrayIconManager(Window mainWindow)
    {
        _mainWindow = mainWindow;
        InitializeTrayIcon();
    }
    
    private void InitializeTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "EasyMeeting - Ready",
            Visibility = Visibility.Visible,
            Icon = CreateAppIcon()
        };
        
        var contextMenu = new System.Windows.Controls.ContextMenu();
        
        _startRecordingItem = new System.Windows.Controls.MenuItem { Header = "Start Recording" };
        _startRecordingItem.Click += (s, e) => StartRecordingRequested?.Invoke(this, EventArgs.Empty);
        
        _stopRecordingItem = new System.Windows.Controls.MenuItem { Header = "Stop Recording" };
        _stopRecordingItem.Click += (s, e) => StopRecordingRequested?.Invoke(this, EventArgs.Empty);
        _stopRecordingItem.IsEnabled = false;
        
        _recordingModeItem = new System.Windows.Controls.MenuItem { Header = "Recording Mode: Both" };
        _recordingModeItem.Click += (s, e) => CycleRecordingMode();
        
        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
        settingsItem.Click += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        
        var showItem = new System.Windows.Controls.MenuItem { Header = "Show" };
        showItem.Click += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
        
        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        
        contextMenu.Items.Add(_startRecordingItem);
        contextMenu.Items.Add(_stopRecordingItem);
        contextMenu.Items.Add(_recordingModeItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(exitItem);
        
        _trayIcon.ContextMenu = contextMenu;
        _trayIcon.TrayMouseDoubleClick += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
        
        _animationTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _animationTimer.Tick += AnimationTimer_Tick;
    }
    
    private Icon CreateAppIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(System.Drawing.Color.Transparent);
        
        using var micBrush = new SolidBrush(Color.FromArgb(0, 120, 212));
        graphics.FillEllipse(micBrush, 4, 2, 24, 28);
        
        using var innerBrush = new SolidBrush(Color.White);
        graphics.FillRectangle(innerBrush, 13, 8, 6, 12);
        graphics.FillEllipse(innerBrush, 11, 18, 10, 10);
        
        using var standBrush = new SolidBrush(Color.FromArgb(0, 120, 212));
        graphics.FillRectangle(standBrush, 14, 26, 4, 4);
        
        IntPtr hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }
    
    private void CycleRecordingMode()
    {
        _currentMode = _currentMode switch
        {
            AudioSourceType.Both => AudioSourceType.SystemAudio,
            AudioSourceType.SystemAudio => AudioSourceType.Microphone,
            AudioSourceType.Microphone => AudioSourceType.Both,
            _ => AudioSourceType.Both
        };
        
        UpdateModeMenuText();
        RecordingModeChanged?.Invoke(this, _currentMode);
    }
    
    private void UpdateModeMenuText()
    {
        if (_recordingModeItem != null)
        {
            _recordingModeItem.Header = _currentMode switch
            {
                AudioSourceType.SystemAudio => "Recording Mode: System Audio",
                AudioSourceType.Microphone => "Recording Mode: Microphone",
                AudioSourceType.Both => "Recording Mode: Both",
                _ => "Recording Mode: Both"
            };
        }
    }
    
    private void UpdateMenuStates()
    {
        if (_startRecordingItem != null)
            _startRecordingItem.IsEnabled = !_isRecording;
        if (_stopRecordingItem != null)
            _stopRecordingItem.IsEnabled = _isRecording;
    }
    
    public void StartAnimation()
    {
        _isAnimating = true;
        _animationTimer?.Start();
        UpdateTooltip("EasyMeeting - Recording...");
    }
    
    public void StopAnimation()
    {
        _isAnimating = false;
        _animationTimer?.Stop();
        _animationFrame = 0;
        UpdateTooltip("EasyMeeting - Ready");
    }
    
    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        _animationFrame = (_animationFrame + 1) % 2;
        if (_trayIcon != null && _isAnimating)
        {
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);
            
            Color circleColor = _animationFrame == 0 ? Color.FromArgb(209, 52, 56) : Color.FromArgb(255, 100, 100);
            using var micBrush = new SolidBrush(circleColor);
            graphics.FillEllipse(micBrush, 4, 2, 24, 28);
            
            using var innerBrush = new SolidBrush(Color.White);
            graphics.FillRectangle(innerBrush, 13, 8, 6, 12);
            graphics.FillEllipse(innerBrush, 11, 18, 10, 10);
            
            using var standBrush = new SolidBrush(circleColor);
            graphics.FillRectangle(standBrush, 14, 26, 4, 4);
            
            IntPtr hIcon = bitmap.GetHicon();
            _trayIcon.Icon = Icon.FromHandle(hIcon);
        }
    }
    
    public void UpdateTooltip(string text)
    {
        if (_trayIcon != null)
            _trayIcon.ToolTipText = text;
    }
    
    public void Dispose()
    {
        _animationTimer?.Stop();
        _trayIcon?.Dispose();
    }
}
