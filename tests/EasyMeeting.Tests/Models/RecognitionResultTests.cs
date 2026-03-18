using Xunit;
using EasyMeeting.Models;

namespace EasyMeeting.Tests.Models;

public class RecognitionResultTests
{
    [Fact]
    public void Constructor_StoresValues()
    {
        var result = new RecognitionResult("test text", TimeSpan.FromSeconds(5), false);
        
        Assert.Equal("test text", result.Text);
        Assert.Equal(5, result.Duration.TotalSeconds);
        Assert.False(result.IsFinal);
    }
}
