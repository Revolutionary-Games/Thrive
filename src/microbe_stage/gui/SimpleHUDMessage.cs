/// <summary>
///   A simple HUD message for the player. Can only combine with messages with the exact same text.
/// </summary>
public class SimpleHUDMessage : IHUDMessage
{
    private readonly string message;

    public SimpleHUDMessage(string message, DisplayDuration duration = DisplayDuration.Normal)
    {
        this.message = message;
        Duration = duration;
    }

    public int Multiplier { get; private set; } = 1;
    public DisplayDuration Duration { get; }

    public float TimeRemaining { get; set; }
    public float OriginalTimeRemaining { get; set; }
    public float TotalDisplayedTime { get; set; }

    public bool IsSameMessage(IHUDMessage other)
    {
        if (other is SimpleHUDMessage otherSimple)
        {
            return message == otherSimple.message;
        }

        return false;
    }

    public void UpdateFromOtherMessage(IHUDMessage other)
    {
        ++Multiplier;
    }

    public override string ToString()
    {
        return message;
    }
}
