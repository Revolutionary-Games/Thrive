public interface IHUDMessage
{
    /// <summary>
    ///   If over 1 the message is displayed in a way that shows that this message happened multiple times
    /// </summary>
    public int Multiplier { get; }

    public DisplayDuration Duration { get; }

    // The following variables are used by HUD messages to keep track of this message
    public float TimeRemaining { get; set; }
    public float OriginalTimeRemaining { get; set; }

    /// <summary>
    ///   Used to track when a really long-lived message should be deleted
    /// </summary>
    public float TotalDisplayedTime { get; set; }

    /// <summary>
    ///   Checks if this and the other message can be shown that that this message happened multiple times
    /// </summary>
    /// <param name="other">The other message to compare against</param>
    /// <returns>True when these are the same and <see cref="UpdateFromOtherMessage"/> can be called</returns>
    public bool IsSameMessage(IHUDMessage other);

    /// <summary>
    ///   Updates this message with another message. For example by incrementing the <see cref="Multiplier"/> or doing
    ///   something smarter.
    /// </summary>
    /// <param name="other">The other to merge data from</param>
    public void UpdateFromOtherMessage(IHUDMessage other);

    public string ToString();
}
