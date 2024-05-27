using Godot;

/// <summary>
///   A page that can be opened in the Thriveopedia. This doesn't implement <see cref="IThriveopediaPage"/> as
///   Godot node types can no longer be abstract.
/// </summary>
[GodotAbstract]
public partial class ThriveopediaPage : PanelContainer
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

    protected ThriveopediaPage()
    {
    }

    [Signal]
    public delegate void OnSceneChangedEventHandler();

    /// <summary>
    ///   Whether this page is initially collapsed in the page tree to save space.
    /// </summary>
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

    public Node PageNode => this;

    public override void _Ready()
    {
        base._Ready();

        // If we're not displaying the background, show a blank panel instead
        if (!DisplayBackground)
            AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
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
    public virtual void OnTranslationsChanged()
    {
    }
}
