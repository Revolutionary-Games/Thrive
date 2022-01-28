/// <summary>
///   A text-based description of what has happened in a game environment. Decorated with an icon if there's any.
/// </summary>
public class GameEventDescription
{
    public GameEventDescription(LocalizedString description, string iconPath, bool highlighted)
    {
        Description = description;
        IconPath = iconPath;
        Highlighted = highlighted;
    }

    /// <summary>
    ///   The text description of this event
    /// </summary>
    public LocalizedString Description { get; private set; }

    /// <summary>
    ///   The resource path to the associated icon
    /// </summary>
    public string IconPath { get; private set; }

    /// <summary>
    ///   If true, this event will be highlighted in a timeline UI
    /// </summary>
    public bool Highlighted { get; private set; }
}
