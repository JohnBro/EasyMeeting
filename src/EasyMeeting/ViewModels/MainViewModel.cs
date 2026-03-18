using System.ComponentModel;

namespace EasyMeeting.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private bool _isRecording;
    private string _statusText = "Ready";
    private string _transcript = string.Empty;
    
    public bool IsRecording
    {
        get => _isRecording;
        set => SetField(ref _isRecording, value);
    }
    
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }
    
    public string Transcript
    {
        get => _transcript;
        set => SetField(ref _transcript, value);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
