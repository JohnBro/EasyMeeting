using Xunit;
using EasyMeeting.Models;

namespace EasyMeeting.Tests.Models;

public class AppConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new AppConfig();
        
        Assert.Equal(SpeechEngineType.Whisper, config.SpeechEngine);
        Assert.Equal(AudioSourceType.Both, config.AudioSource);
        Assert.Equal(180, config.MaxRecordingMinutes);
        Assert.Equal("en", config.SummaryLanguage);
        Assert.Contains(".easymeeting", config.OutputDirectory);
    }
    
    [Fact]
    public void JsonSerialization_RoundTrips()
    {
        var config = new AppConfig { SpeechEngine = SpeechEngineType.SAPI };
        var json = System.Text.Json.JsonSerializer.Serialize(config);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(config.SpeechEngine, deserialized.SpeechEngine);
    }
}
