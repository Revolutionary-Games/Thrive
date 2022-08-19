using Godot;
using System;
using System.Collections.Generic;

public class Thriveopedia : ControlWithInput
{
    [Export]
    public NodePath BackButtonPath = null!;

    [Export]
    public NodePath HomeTabPath = null!;

    [Export]
    public NodePath MuseumTabPath = null!;

    [Export]
    public NodePath StatisticsTabPath = null!;

    [Export]
    public NodePath StatisticsButtonPath = null!;

    [Export]
    public NodePath EvolutionaryTreePath = null!;

    private Button backButton = null!;
    private Control homeTab = null!;
    private Control museumTab = null!;
    private Control statisticsTab = null!;
    private Button statisticsButton = null!;

    private EvolutionaryTree evolutionaryTree = null!;

    private GameProperties? currentGame;

    private ThriveopediaTab selectedTab;

    private Stack<ThriveopediaTab> tabHistory = new();

    [Signal]
    public delegate void OnThriveopediaClosed();

    public GameProperties? CurrentGame
    {
        get => currentGame;
        set
        {
            currentGame = value;

            UpdateAvailableTabs();

            if (currentGame == null)
                return;

            evolutionaryTree.Init(currentGame.GameWorld.PlayerSpecies);
        }
    }

    public ThriveopediaTab SelectedTab
    {
        get => selectedTab;
        set
        {
            selectedTab = value;

            backButton.Disabled = tabHistory.Count <= 1;
        }
    }

    public enum ThriveopediaTab
    {
        Home,
        Museum,
        Statistics
    }

    public override void _Ready()
    {
        backButton = GetNode<Button>(BackButtonPath);
        homeTab = GetNode<Control>(HomeTabPath);
        museumTab = GetNode<Control>(MuseumTabPath);
        statisticsTab = GetNode<Control>(StatisticsTabPath);
        statisticsButton = GetNode<Button>(StatisticsButtonPath);

        evolutionaryTree = GetNode<EvolutionaryTree>(EvolutionaryTreePath);

        tabHistory.Push(ThriveopediaTab.Home);
        SelectedTab = ThriveopediaTab.Home;
        UpdateAvailableTabs();
    }

    /// <summary>
    ///   Opens the Thriveopedia from the main menu
    /// </summary>
    public void OpenFromMainMenu()
    {
        // Shouldn't do anything if already open
        if (Visible)
            return;

        Show();
    }

    /// <summary>
    ///   Opens the Thriveopedia from a particular game
    /// </summary>
    public void OpenInGame(GameProperties game)
    {
        // Shouldn't do anything if already open
        if (Visible)
            return;

        CurrentGame = game;

        Show();
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.SUBMENU_CANCEL_PRIORITY)]
    public bool OnEscapePressed()
    {
        // Only handle keypress when visible
        if (!Visible)
            return false;

        Exit();

        // If opened in a game, hide the pause menu too
        return currentGame == null;
    }

    private void UpdateAvailableTabs()
    {
        // Don't enable game statistics if we have no game
        statisticsButton.Disabled = CurrentGame == null;
    }

    /// <summary>
    ///   Changes the active tab that is displayed, or returns if the tab is already active
    ///   TODO: replace with a browser-like tab system
    /// </summary>
    private void ChangeTab(string newTabName, bool addToHistory)
    {
        // Convert from the string binding to an enum.
        ThriveopediaTab selection = (ThriveopediaTab)Enum.Parse(typeof(ThriveopediaTab), newTabName);

        // Pressing the same button that's already active, so just return
        if (selection == SelectedTab)
            return;

        homeTab.Hide();
        museumTab.Hide();
        statisticsTab.Hide();

        switch (selection)
        {
            case ThriveopediaTab.Home:
                homeTab.Show();
                break;
            case ThriveopediaTab.Museum:
                museumTab.Show();
                break;
            case ThriveopediaTab.Statistics:
                statisticsTab.Show();
                break;
            default:
                GD.PrintErr("Invalid tab");
                break;
        }

        GUICommon.Instance.PlayButtonPressSound();

        if (addToHistory)
            tabHistory.Push(selection);

        SelectedTab = selection;
    }

    private void OnHomePressed()
    {
        ChangeTab("Home", true);
    }

    private void OnBackPressed()
    {
        tabHistory.Pop();
        ChangeTab(tabHistory.Peek().ToString(), false);
    }

    private void OnMuseumPressed()
    {
        ChangeTab("Museum", true);
    }

    private void OnStatisticsPressed()
    {
        ChangeTab("Statistics", true);
    }

    private void OnClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        Exit();
    }

    private void Exit()
    {
        tabHistory.Clear();

        EmitSignal(nameof(OnThriveopediaClosed));
    }
}
