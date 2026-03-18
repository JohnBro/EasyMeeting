using Xunit;
using EasyMeeting.Services.Recognition;

namespace EasyMeeting.Tests.Services.Recognition;

public class ISpeechRecognitionServiceTests
{
    [Fact]
    public void ISpeechRecognitionService_HasRequiredMembers()
    {
        var type = typeof(ISpeechRecognitionService);
        
        Assert.True(type.IsInterface);
        Assert.NotNull(type.GetMethod("InitializeAsync"));
        Assert.NotNull(type.GetMethod("RecognizeAsync"));
        Assert.NotNull(type.GetMethod("DisposeAsync"));
    }
}
