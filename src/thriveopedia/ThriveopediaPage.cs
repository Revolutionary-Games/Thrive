﻿using Godot;

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

    /// <summary>
    ///   The internal name of this page. Must be PascalCase to open the Godot scene correctly.
    /// </summary>
    public abstract string PageName { get; }

    /// <summary>
    ///   The translated name of this page.
    /// </summary>
    public abstract string TranslatedPageName { get; }

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

    /// <summary>
    ///   Performs intensive page-specific logic to rebuild views when the Thriveopedia is opened.
    /// </summary>
    public abstract void OnThriveopediaOpened();

    /// <summary>
    ///   Runs any page-specific logic relating to a newly set game in progress.
    /// </summary>
    public abstract void UpdateCurrentWorldDetails();

    /// <summary>
    ///   Runs any page-specific logic when the page tree is collapsed/expanded.
    /// </summary>
    /// <param name="collapsed">Whether the page tree is currently collapsed</param>
    public abstract void OnNavigationPanelSizeChanged(bool collapsed);
}
