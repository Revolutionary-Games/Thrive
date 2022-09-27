using Godot;
using System.Collections.Generic;
using System.Linq;

public class Thriveopedia : ControlWithInput
{
    [Export]
    public NodePath BackButtonPath = null!;

    [Export]
    public NodePath ForwardButtonPath = null!;

    [Export]
    public NodePath PageContainerPath = null!;

    [Export]
    public NodePath PageTreePath = null!;

    [Export]
    public NodePath HomePagePath = null!;

    private MarginContainer pageContainer = null!;
    private Button backButton = null!;
    private Button forwardButton = null!;
    private Tree pageTree = null!;
    private ThriveopediaHomePage homePage = null!;

    private GameProperties? currentGame;

    private ThriveopediaPage selectedPage = null!;

    private List<ThriveopediaPage> allPages = new();
    private Stack<ThriveopediaPage> pageHistory = new();
    private Stack<ThriveopediaPage> pageFuture = new();

    [Signal]
    public delegate void OnThriveopediaClosed();

    public ThriveopediaPage SelectedPage
    {
        get => selectedPage;
        set
        {
            // Hide the last page and show the new page
            if (selectedPage != null)
                selectedPage.Hide();

            selectedPage = value;
            selectedPage.Show();

            // The home page is always the first in the history, so ignore it
            backButton.Disabled = pageHistory.Count <= 1;
            forwardButton.Disabled = pageFuture.Count == 0;
        }
    }

    public GameProperties? CurrentGame
    {
        get => currentGame;
        set
        {
            currentGame = value;

            foreach (var page in allPages)
                page.CurrentGame = currentGame;
        }
    }

    public override void _Ready()
    {
        backButton = GetNode<Button>(BackButtonPath);
        forwardButton = GetNode<Button>(ForwardButtonPath);
        pageContainer = GetNode<MarginContainer>(PageContainerPath);
        pageTree = GetNode<Tree>(PageTreePath);

        // Create and hide a blank root to avoid home being used as the root
        pageTree.CreateItem();
        pageTree.HideRoot = true;

        // Keep a special reference to the home page
        homePage = GetNode<ThriveopediaHomePage>(HomePagePath);
        homePage.OpenPage = ChangePage;
        homePage.AddPageAsChild = AddPage;
        allPages.Add(homePage);
        AddPageToTree(homePage, null);

        AddPage("ThriveopediaCurrentWorldPage");
        AddPage("ThriveopediaMuseumPage");
        AddPage("ThriveopediaEvolutionaryTreePage");

        pageHistory.Push(homePage);
        SelectedPage = homePage;
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

    private ThriveopediaPage GetPage(string? name)
    {
        return allPages.FirstOrDefault(p => p.PageName == name);
    }

    private void AddPage(string name, string? parentName = null)
    {
        var scene = GD.Load<PackedScene>($"res://src/gui_common/thriveopedia/{name}.tscn");
        var page = (ThriveopediaPage)scene.Instance();
        page.OpenPage = ChangePage;
        page.AddPageAsChild = AddPage;
        pageContainer.AddChild(page);
        allPages.Add(page);
        page.Hide();

        AddPageToTree(page, parentName);
    }

    private void AddPageToTree(ThriveopediaPage page, string? parentName)
    {
        var parent = GetPage(parentName);
        var pageInTree = pageTree.CreateItem(parent?.PageTreeItem);
        page.PageTreeItem = pageInTree;
        pageInTree.SetMeta("name", page.PageName);
        pageInTree.SetText(0, page.TranslatedPageName);
    }

    private void OnPageSelectedFromPageTree()
    {
        var name = (string)pageTree.GetSelected().GetMeta("name");
        ChangePage(name);
    }

    private void ChangePage(string pageName)
    {
        // By default, assume we're navigating to this page normally
        ChangePage(pageName, true, true);
    }

    private void ChangePage(string pageName, bool addToHistory, bool clearFuture)
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (pageName == selectedPage.PageName)
            return;

        var page = GetPage(pageName);

        if (page == null)
        {
            GD.PrintErr($"No page with name {pageName} exists");
            return;
        }

        if (addToHistory)
            pageHistory.Push(page);

        if (clearFuture)
            pageFuture.Clear();

        SelectedPage = page;
    }

    private void OnHomePressed()
    {
        ChangePage(homePage.PageName);
    }

    private void OnBackPressed()
    {
        pageFuture.Push(pageHistory.Pop());
        ChangePage(pageHistory.Peek().PageName, false, false);
    }

    private void OnForwardPressed()
    {
        ChangePage(pageFuture.Pop().PageName, true, false);
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
