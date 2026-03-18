using Xunit;
using EasyMeeting.Models;
using EasyMeeting.Services.Audio;

namespace EasyMeeting.Tests.Services.Audio;

public class AudioMixerTests
{
    [Fact]
    public void Mix_CombinesTwoSamples()
    {
        var mixer = new AudioMixer();
        var left = new AudioSample(new float[] { 1.0f, 0.5f }, 16000, 1);
        var right = new AudioSample(new float[] { 0.5f, 0.25f }, 16000, 1);
        
        var result = mixer.Mix(left, right);
        
        Assert.Equal(2, result.Data.Length);
        Assert.Equal(0.75f, result.Data[0], 2);
        Assert.Equal(0.375f, result.Data[1], 2);
    }
    
    [Fact]
    public void Mix_HandlesDifferentLengths()
    {
        var mixer = new AudioMixer();
        var left = new AudioSample(new float[] { 1.0f }, 16000, 1);
        var right = new AudioSample(new float[] { 0.5f, 0.25f }, 16000, 1);
        
        var result = mixer.Mix(left, right);
        
        Assert.Equal(2, result.Data.Length);
        Assert.Equal(0.5f, result.Data[0], 2);
        Assert.Equal(0.125f, result.Data[1], 2);
    }
    
    [Fact]
    public void Mix_ThrowsOnMismatchedSampleRates()
    {
        var mixer = new AudioMixer();
        var left = new AudioSample(new float[] { 1.0f }, 16000, 1);
        var right = new AudioSample(new float[] { 0.5f }, 48000, 1);
        
        Assert.Throws<InvalidOperationException>(() => mixer.Mix(left, right));
    }
}
