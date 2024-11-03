/// <summary>
///   A text-based description of what has happened in a game environment. Decorated with an icon if there's any.
/// </summary>
public class GameEventDescription
{
    public GameEventDescription(LocalizedString description, string? iconPath, bool highlighted, bool showInReport)
    {
        Description = description;
        IconPath = iconPath;
        Highlighted = highlighted;
        ShowInReport = showInReport;
    }

    /// <summary>
    ///   The text description of this event
    /// </summary>
    public LocalizedString Description { get; private set; }

    /// <summary>
    ///   The resource path to the associated icon
    /// </summary>
    public string? IconPath { get; private set; }

    /// <summary>
    ///   If true, this event will be highlighted in the timeline UI
    /// </summary>
    public bool Highlighted { get; private set; }

    /// <summary>
    ///   Some events show up in report tab
    /// </summary>
    public bool ShowInReport { get; private set; }
}
