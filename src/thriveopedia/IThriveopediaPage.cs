using Godot;

public interface IThriveopediaPage
{
    /// <summary>
    ///   The internal name of this page. If this page is the only instance of a specific Godot scene, must be
    ///   PascalCase to open the scene correctly.
    /// </summary>
    public string PageName { get; }

    /// <summary>
    ///   The translated name of this page.
    /// </summary>
    public string TranslatedPageName { get; }

    /// <summary>
    ///   The internal name of the parent of this page in the tree, or null if this page is at the top level.
    /// </summary>
    public string? ParentPageName { get; }

    /// <summary>
    ///   Whether this page is initially collapsed in the page tree to save space.
    /// </summary>
    public bool StartsCollapsed { get; }

    /// <summary>
    ///   Details for the game currently in progress. Null if opened from the main menu.
    /// </summary>
    public GameProperties? CurrentGame { get; set; }

    public Node PageNode { get; }

    public void Hide();
    public void Show();

    public void OnThriveopediaOpened();

    public void OnNavigationPanelSizeChanged(bool collapsed);
}
