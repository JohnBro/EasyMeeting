namespace EasyMeeting.Models;

public record RecognitionResult(string Text, TimeSpan Duration, bool IsFinal);
