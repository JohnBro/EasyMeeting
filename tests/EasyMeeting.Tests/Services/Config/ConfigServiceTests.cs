using Xunit;
using EasyMeeting.Services.Config;
using EasyMeeting.Models;

namespace EasyMeeting.Tests.Services.Config;

public class ConfigServiceTests
{
    [Fact]
    public void Load_ReturnsDefaultConfig_WhenFileDoesNotExist()
    {
        var service = new ConfigService();
        var config = service.Load();
        
        Assert.NotNull(config);
        Assert.Equal(SpeechEngineType.Whisper, config.SpeechEngine);
    }
    
    [Fact]
    public void Current_ReturnsLoadedConfig()
    {
        var service = new ConfigService();
        var config = service.Current;
        
        Assert.NotNull(config);
    }
    
    [Fact]
    public void Save_UpdatesCurrent()
    {
        var service = new ConfigService();
        var original = service.Current;
        
        var newConfig = new AppConfig
        {
            SpeechEngine = SpeechEngineType.SAPI,
            AudioSource = AudioSourceType.Microphone
        };
        
        service.Save(newConfig);
        
        Assert.Equal(SpeechEngineType.SAPI, service.Current.SpeechEngine);
        Assert.Equal(AudioSourceType.Microphone, service.Current.AudioSource);
    }
}
