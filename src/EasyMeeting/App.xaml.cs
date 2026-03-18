using System.Runtime.InteropServices;
using System.Windows;

namespace EasyMeeting;

public partial class App : Application
{
    private Infrastructure.TrayIconManager? _trayIconManager;
    private Infrastructure.HotkeyManager? _hotkeyManager;
    private UI.Views.PreviewWindow? _previewWindow;
    private static bool _isDebugMode;
    
    public static bool IsDebugMode => _isDebugMode;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        _isDebugMode = e.Args.Length > 0 && e.Args.Contains("-d", System.StringComparer.OrdinalIgnoreCase);
        
        if (_isDebugMode)
        {
            AllocConsole();
            DebugService.Initialize();
            DebugService.Log("=== EasyMeeting Debug Mode Started ===");
            DebugService.Log($"Args: {string.Join(", ", e.Args)}");
        }
        
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var existingProcesses = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);
        if (existingProcesses.Length > 1)
        {
            if (_isDebugMode) DebugService.Log("Another instance is already running. Exiting.");
            MessageBox.Show("EasyMeeting is already running.", "EasyMeeting", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        
        DebugService.Log("Creating MainWindow...");
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        
        DebugService.Log("Initializing TrayIconManager...");
        _trayIconManager = new Infrastructure.TrayIconManager(mainWindow);
        mainWindow.SetTrayIconManager(_trayIconManager);
        
        DebugService.Log("Initializing HotkeyManager...");
        _hotkeyManager = new Infrastructure.HotkeyManager(mainWindow);
        _previewWindow = new UI.Views.PreviewWindow();
        
        _hotkeyManager.ShowHideRequested += (s, args) =>
        {
            DebugService.Log("Hotkey: Preview window toggle requested");
            if (_previewWindow.IsVisible)
                _previewWindow.Hide();
            else
            {
                _previewWindow.Show();
                _previewWindow.Activate();
            }
        };
        
        _hotkeyManager.RegisterDefaultHotkey();
        
        mainWindow.StateChanged += (s, args) =>
        {
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.Hide();
        };
        
        mainWindow.Closing += (s, args) =>
        {
            args.Cancel = true;
            mainWindow.Hide();
        };
        
        DebugService.Log("Startup complete. Application running.");
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        DebugService.Log("Application exiting...");
        _hotkeyManager?.Unregister();
        _hotkeyManager?.Dispose();
        _trayIconManager?.Dispose();
        _previewWindow?.Close();
        
        if (_isDebugMode)
        {
            DebugService.Log("=== EasyMeeting Debug Mode Ended ===");
            Console.WriteLine("\nPress any key to close...");
            Console.ReadKey(true);
            FreeConsole();
        }
        
        base.OnExit(e);
    }
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();
}

public static class DebugService
{
    private static bool _initialized;
    
    public static void Initialize()
    {
        _initialized = true;
        Console.OutputEncoding = System.Text.Encoding.UTF8;
    }
    
    public static void Log(string message)
    {
        if (!_initialized || !App.IsDebugMode) return;
        
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {message}";
        Console.WriteLine(logLine);
    }
    
    public static void LogError(string message, Exception? ex = null)
    {
        if (!_initialized || !App.IsDebugMode) return;
        
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] ERROR: {message}";
        Console.WriteLine(logLine);
        if (ex != null)
        {
            Console.WriteLine($"         Exception: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
