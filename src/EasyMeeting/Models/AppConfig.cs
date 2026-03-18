using System.IO;

namespace EasyMeeting.Models;

public enum SpeechEngineType { Whisper, SAPI }
public enum AudioSourceType { SystemAudio, Microphone, Both }

public class AppConfig
{
    public SpeechEngineType SpeechEngine { get; set; } = SpeechEngineType.Whisper;
    public AudioSourceType AudioSource { get; set; } = AudioSourceType.Both;
    public int MaxRecordingMinutes { get; set; } = 180;
    public string SummaryLanguage { get; set; } = "en";
    public string UiLanguage { get; set; } = "en";
    public string OutputDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".easymeeting");
    public string WhisperModelPath { get; set; } = string.Empty;
    public LlmConfig Llm { get; set; } = new();
}

public class LlmConfig
{
    public string Provider { get; set; } = "openai";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public string SummaryPrompt { get; set; } = "Summarize the following transcription in {language} language:\n\n{text}";
}
