using System.Windows;
using System.Windows.Controls;
using EasyMeeting.Models;
using EasyMeeting.Services.Config;
using Microsoft.Win32;

namespace EasyMeeting.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly AppConfig _config;
    
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
        
        _configService.Save(_config);
        
        DialogResult = true;
        Close();
    }
}
