using System;
using Godot;

/// <summary>
///   A page that can be opened in the Thriveopedia.
/// </summary>
public abstract class ThriveopediaPage : PanelContainer
{
    /// <summary>
    ///   Whether this page should display the default panel background.
    /// </summary>
    [Export]
    public bool DisplayBackground = true;

    /// <summary>
    ///   Details for the game currently in progress. Null if opened from the main menu.
    /// </summary>
    private GameProperties? currentGame;

    [Signal]
    public delegate void OnSceneChanged();

    public Action<string> ChangePage { get; set; } = null!;

    /// <summary>
    ///   The internal name of this page. If this page is the only instance of a specific Godot scene, must be PascalCase to open the scene correctly.
    /// </summary>
    public abstract string PageName { get; }

    /// <summary>
    ///   The translated name of this page.
    /// </summary>
    public abstract string TranslatedPageName { get; }

    /// <summary>
    ///   The internal name of the parent of this page in the tree, or null if this page is at the top level.
    /// </summary>
    public abstract string? ParentPageName { get; }

    public virtual bool StartsCollapsed => false;

    /// <summary>
    ///   Details for the game currently in progress. Null if opened from the main menu. When set, runs any
    ///   page-specific logic relating to the new game details.
    /// </summary>
    public GameProperties? CurrentGame
    {
        get => currentGame;
        set
        {
            currentGame = value;

            UpdateCurrentWorldDetails();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // If we're not displaying the background, show a blank panel instead
        if (!DisplayBackground)
            AddStyleboxOverride("panel", new StyleBoxEmpty());
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
            OnTranslationChanged();
    }

    /// <summary>
    ///   Performs intensive page-specific logic to rebuild views when the Thriveopedia is opened.
    /// </summary>
    public virtual void OnThriveopediaOpened()
    {
    }

    /// <summary>
    ///   Runs any page-specific logic relating to a newly set game in progress.
    /// </summary>
    public virtual void UpdateCurrentWorldDetails()
    {
    }

    /// <summary>
    ///   Runs any page-specific logic when the page tree is collapsed/expanded.
    /// </summary>
    /// <param name="collapsed">Whether the page tree is currently collapsed</param>
    public virtual void OnNavigationPanelSizeChanged(bool collapsed)
    {
    }

    /// <summary>
    ///   Called when <see cref="Node.NotificationTranslationChanged"/> is received.
    /// </summary>
    public virtual void OnTranslationChanged()
    {
    }
}
