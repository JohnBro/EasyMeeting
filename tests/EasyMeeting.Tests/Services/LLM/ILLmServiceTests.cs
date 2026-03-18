using Xunit;
using EasyMeeting.Services.LLM;

namespace EasyMeeting.Tests.Services.LLM;

public class ILLmServiceTests
{
    [Fact]
    public void ILLmService_HasRequiredMembers()
    {
        var type = typeof(ILLmService);
        
        Assert.True(type.IsInterface);
        Assert.NotNull(type.GetMethod("SummarizeStreamingAsync"));
        Assert.NotNull(type.GetMethod("SummarizeAsync"));
    }
}
