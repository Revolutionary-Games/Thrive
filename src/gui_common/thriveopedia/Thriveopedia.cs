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
    public NodePath PageTreeContainerPath = null!;

    [Export]
    public NodePath PageTreeSeparatorPath = null!;

    [Export]
    public NodePath PageTreePath = null!;

    [Export]
    public NodePath HomePagePath = null!;

    private TextureButton backButton = null!;
    private TextureButton forwardButton = null!;
    private MarginContainer pageContainer = null!;
    private PanelContainer pageTreeContainer = null!;
    private Control pageTreeSeparator = null!;
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
            if (value == currentGame)
                return;

            currentGame = value;

            if (currentGame != null)
            {
                AddPage("CurrentWorld");
                AddPage("PatchMap", "CurrentWorld");
                AddPage("EvolutionaryTree", "CurrentWorld");
            }

            foreach (var page in allPages.ToList())
                page.CurrentGame = currentGame;
        }
    }

    public override void _Ready()
    {
        backButton = GetNode<TextureButton>(BackButtonPath);
        forwardButton = GetNode<TextureButton>(ForwardButtonPath);
        pageContainer = GetNode<MarginContainer>(PageContainerPath);
        pageTreeContainer = GetNode<PanelContainer>(PageTreeContainerPath);
        pageTreeSeparator = GetNode<Control>(PageTreeSeparatorPath);
        pageTree = GetNode<Tree>(PageTreePath);

        // Create and hide a blank root to avoid home being used as the root
        pageTree.CreateItem();
        pageTree.HideRoot = true;

        // Keep a special reference to the home page
        homePage = GetNode<ThriveopediaHomePage>(HomePagePath);
        allPages.Add(homePage);
        AddPageToTree(homePage, null);

        AddPage("Museum");

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

    public void ChangePage(string pageName)
    {
        // By default, assume we're navigating to this page normally
        ChangePage(pageName, true, true);
    }

    private ThriveopediaPage GetPage(string? name)
    {
        return allPages.FirstOrDefault(p => p.PageName == name);
    }

    private void AddPage(string name, string? parentName = null)
    {
        // Avoid adding duplicate pages
        if (allPages.Any(p => p.PageName == name))
            return;

        var scene = GD.Load<PackedScene>($"res://src/gui_common/thriveopedia/Thriveopedia{name}Page.tscn");
        var page = (ThriveopediaPage)scene.Instance();
        pageContainer.AddChild(page);
        allPages.Add(page);
        page.Hide();

        AddPageToTree(page, parentName);
    }

    private void AddPageToTree(ThriveopediaPage page, string? parentName = null)
    {
        var parent = GetPage(parentName);
        var pageInTree = pageTree.CreateItem(parent?.PageTreeItem);
        page.PageTreeItem = pageInTree;
        pageInTree.SetMeta("name", page.PageName);

        // Godot doesn't appear to have a left margin for text in items, so add some manual padding
        pageInTree.SetText(0, "  " + page.TranslatedPageName);
    }

    private void OnPageSelectedFromPageTree()
    {
        var name = (string)pageTree.GetSelected().GetMeta("name");
        ChangePage(name);
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

    private void OnCollapseTreePressed()
    {
        pageTreeContainer.Visible = !pageTreeContainer.Visible;
        pageTreeSeparator.Visible = !pageTreeSeparator.Visible;
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
