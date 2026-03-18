using Xunit;
using EasyMeeting.Models;

namespace EasyMeeting.Tests.Models;

public class AudioSampleTests
{
    [Fact]
    public void Duration_IsCalculatedCorrectly()
    {
        var sample = new AudioSample(new float[16000], 16000, 1);
        
        Assert.Equal(1, sample.Duration.TotalSeconds);
    }
    
    [Fact]
    public void Constructor_StoresValues()
    {
        var data = new float[] { 0.5f, -0.5f };
        var sample = new AudioSample(data, 16000, 1);
        
        Assert.Equal(2, sample.Data.Length);
        Assert.Equal(16000, sample.SampleRate);
        Assert.Equal(1, sample.Channels);
    }
}
