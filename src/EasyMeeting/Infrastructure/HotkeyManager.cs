using System.Windows;
using NHotkey;
using NHotkey.Wpf;

namespace EasyMeeting.Infrastructure;

public class HotkeyManager : IDisposable
{
    private readonly Window _window;
    
    public event EventHandler? ShowHideRequested;
    
    public HotkeyManager(Window window)
    {
        _window = window;
    }
    
    public void RegisterDefaultHotkey()
    {
        try
        {
            NHotkey.Wpf.HotkeyManager.Current.AddOrReplace("ShowHide", 
                System.Windows.Input.Key.Space, 
                System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift, 
                OnHotkeyPressed);
        }
        catch (HotkeyAlreadyRegisteredException)
        {
        }
    }
    
    private void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        ShowHideRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }
    
    public void Unregister()
    {
        try
        {
            NHotkey.Wpf.HotkeyManager.Current.Remove("ShowHide");
        }
        catch
        {
        }
    }
    
    public void Dispose()
    {
        Unregister();
    }
}
