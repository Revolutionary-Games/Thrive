using Godot;
using System.Collections.Generic;
using System.Linq;

public class Thriveopedia : ControlWithInput
{
    [Export]
    public NodePath BackButtonPath = null!;

    [Export]
    public NodePath PageContainerPath = null!;

    [Export]
    public NodePath HomePagePath = null!;

    private PackedScene museumPageScene = null!;

    private MarginContainer pageContainer = null!;
    private Button backButton = null!;
    private ThriveopediaHomePage homePage = null!;
    private ThriveopediaMuseumPage museumPage = null!;

    private GameProperties? currentGame;

    private ThriveopediaPage selectedPage = null!;

    private List<ThriveopediaPage> allPages = new();
    private Stack<ThriveopediaPage> pageHistory = new();

    [Signal]
    public delegate void OnThriveopediaClosed();

    public ThriveopediaPage SelectedPage
    {
        get => selectedPage;
        set
        {
            // Hide the last page and show the new page
            selectedPage.Hide();
            selectedPage = value;
            selectedPage.Show();

            backButton.Disabled = pageHistory.Count <= 1;
        }
    }

    public GameProperties? CurrentGame
    {
        get => currentGame;
        set
        {
            currentGame = value;

            homePage.CurrentWorldDisabled = CurrentGame == null;
        }
    }

    public override void _Ready()
    {
        backButton = GetNode<Button>(BackButtonPath);
        pageContainer = GetNode<MarginContainer>(PageContainerPath);

        homePage = GetNode<ThriveopediaHomePage>(HomePagePath);
        homePage.OpenPage = ChangePage;
        allPages.Add(homePage);

        museumPageScene = GD.Load<PackedScene>("res://src/gui_common/thriveopedia/ThriveopediaMuseumPage.tscn");
        museumPage = (ThriveopediaMuseumPage)museumPageScene.Instance();
        pageContainer.AddChild(museumPage);
        allPages.Add(museumPage);
        museumPage.Hide();

        pageHistory.Push(homePage);
        selectedPage = homePage;
        homePage.CurrentWorldDisabled = true;
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

    private void ChangePage(string pageName)
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (pageName == selectedPage.PageName)
            return;

        var page = allPages.FirstOrDefault(p => p.PageName == pageName);

        if (page == null)
        {
            GD.PrintErr($"No page with name {pageName} exists");
            return;
        }

        pageHistory.Push(page);

        SelectedPage = page;
    }

    private void ChangePageWithoutAddingToHistory(string pageName)
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (pageName == selectedPage.PageName)
            return;

        var page = allPages.First(p => p.PageName == pageName);

        if (page == null)
        {
            GD.PrintErr($"No page with name {pageName} exists");
            return;
        }

        SelectedPage = page;
    }

    private void OnHomePressed()
    {
        ChangePage(homePage.PageName);
    }

    private void OnBackPressed()
    {
        pageHistory.Pop();
        ChangePageWithoutAddingToHistory(pageHistory.Peek().PageName);
    }

    private void OnStatisticsPressed()
    {
        //ChangePage("Statistics");
    }

    private void OnClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        Exit();
    }

    private void Exit()
    {
        pageHistory.Clear();

        EmitSignal(nameof(OnThriveopediaClosed));
    }
}
