using System.ComponentModel;

namespace EasyMeeting.Models;

public class AudioSample
{
    public float[] Data { get; }
    public int SampleRate { get; }
    public int Channels { get; }

    public AudioSample(float[] data, int sampleRate, int channels)
    {
        Data = data;
        SampleRate = sampleRate;
        Channels = channels;
    }

    public TimeSpan Duration => TimeSpan.FromSeconds((double)Data.Length / SampleRate / Channels);
}
