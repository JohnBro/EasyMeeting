# EasyMeeting

Real-time speech-to-text Windows desktop application with LLM summarization.

## Features

- **Dual Audio Capture**: Record system audio (WASAPI Loopback), microphone, or both simultaneously
- **Speech Recognition Engines**: 
  - Whisper.net (default, local/offline)
  - Windows SAPI (built-in)
- **LLM Summarization**: 
  - OpenAI compatible API
  - Claude API
  - Custom provider support (interface预留)
- **Background Running**: System tray icon with recording animation
- **Live Preview**: Semi-transparent overlay window with adjustable opacity and font size
- **Global Hotkey**: `Ctrl+Shift+Space` to toggle preview window

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- For Whisper: GGML model files (auto-downloaded on first use)

## Build

```bash
# Install .NET 8 SDK if not present
# https://dotnet.microsoft.com/download/dotnet/8.0

# Restore packages
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Publish
dotnet publish -c Release -r win-x64 --self-contained
```

## Configuration

Configuration file is stored at: `%USERPROFILE%\.easymeeting\config.json`

### Example Configuration

```json
{
  "SpeechEngine": "Whisper",
  "AudioSource": "Both",
  "MaxRecordingMinutes": 180,
  "SummaryLanguage": "en",
  "OutputDirectory": "C:\\Users\\<username>\\.easymeeting",
  "WhisperModelPath": "",
  "Llm": {
    "Provider": "openai",
    "ApiKey": "your-api-key-here",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-4o-mini",
    "SummaryPrompt": "Summarize the following transcription in {language} language:\n\n{text}"
  }
}
```

### Configuration Options

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| SpeechEngine | enum | Whisper | Whisper or SAPI |
| AudioSource | enum | Both | SystemAudio, Microphone, or Both |
| MaxRecordingMinutes | int | 180 | Maximum recording duration (3 hours) |
| SummaryLanguage | string | "en" | Language for summary output |
| OutputDirectory | string | ~/.easymeeting | Where to save output files |
| WhisperModelPath | string | "" | Path to custom Whisper model |
| Llm.Provider | string | "openai" | "openai" or "claude" |
| Llm.ApiKey | string | "" | API key for LLM service |
| Llm.BaseUrl | string | OpenAI URL | API endpoint |
| Llm.Model | string | "gpt-4o-mini" | Model name |
| Llm.SummaryPrompt | string | ... | Prompt template with {language} and {text} placeholders |

## Output Files

Recording creates two files in the output directory:

1. **Raw Transcription**: `{yyyy-mm-dd_hh_mm_ss_raw.md}`
   ```markdown
   # Transcription
   
   [transcribed text here]
   ```

2. **Summary**: `{yyyy-mm-dd_hh_mm_ss_summary.md}`
   ```markdown
   # Summary
   
   [LLM generated summary]
   ```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Shift+Space | Toggle preview window |

## Architecture

```
EasyMeeting/
├── Models/           - Data models (AppConfig, AudioSample, etc.)
├── Services/
│   ├── Audio/       - IAudioCaptureService implementations
│   ├── Recognition/  - ISpeechRecognitionService implementations
│   ├── LLM/         - ILLmService implementations
│   ├── Config/      - Configuration management
│   └── Recording/   - Orchestrator tying all services together
├── UI/
│   ├── Views/       - WPF windows
│   └── Controls/     - Custom WPF controls
├── Infrastructure/   - System tray, hotkeys
└── ViewModels/      - MVVM view models
```

## License

MIT
