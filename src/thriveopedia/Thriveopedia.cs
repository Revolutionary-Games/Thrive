using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Central store of game information for the player. Acts like a browser. Can be opened in-game and from the main
///   menu. Some pages relating to a game in progress are only available in-game.
/// </summary>
public partial class Thriveopedia : ControlWithInput
{
    [Export]
    public NodePath? BackButtonPath;

    [Export]
    public NodePath ForwardButtonPath = null!;

    [Export]
    public NodePath PageContainerPath = null!;

    [Export]
    public NodePath PageTreeContainerPath = null!;

    [Export]
    public NodePath PageTreeContainerAnimPath = null!;

    [Export]
    public NodePath PageTitlePath = null!;

    [Export]
    public NodePath ViewOnlineButtonPath = null!;

    [Export]
    public NodePath PageTreePath = null!;

    [Export]
    public NodePath HomePagePath = null!;

    /// <summary>
    ///   All Thriveopedia pages and their associated items in the page tree.
    /// </summary>
    private readonly Dictionary<IThriveopediaPage, TreeItem> allPages = new();

    /// <summary>
    ///   Page history for backwards navigation.
    /// </summary>
    private readonly Stack<IThriveopediaPage> pageHistory = new();

    /// <summary>
    ///   Page future for forwards navigation (if the player has already gone backwards).
    /// </summary>
    private readonly Stack<IThriveopediaPage> pageFuture = new();

#pragma warning disable CA2213
    private TextureButton backButton = null!;
    private TextureButton forwardButton = null!;
    private MarginContainer pageContainer = null!;
    private PanelContainer pageTreeContainer = null!;
    private AnimationPlayer pageTreeContainerAnim = null!;
    private Label pageTitle = null!;
    private Button viewOnlineButton = null!;
    private Tree pageTree = null!;

    /// <summary>
    ///   The home page for the Thriveopedia. Keep a special reference so we can return to it easily.
    /// </summary>
    private ThriveopediaHomePage homePage = null!;
#pragma warning restore CA2213

    private bool treeCollapsed;

    /// <summary>
    ///   Details for the game currently in progress. Null if opened from the main menu.
    /// </summary>
    private GameProperties? currentGame;

    /// <summary>
    ///   The currently open Thriveopedia page.
    /// </summary>
    private IThriveopediaPage? selectedPage;

    /// <summary>
    ///   Flag indicating whether the Thriveopedia has created all wiki pages, since we need to generate them if not.
    /// </summary>
    private bool hasGeneratedWiki;

    [Signal]
    public delegate void OnThriveopediaClosedEventHandler();

    [Signal]
    public delegate void OnSceneChangedEventHandler();

    /// <summary>
    ///   The currently open Thriveopedia page. Defaults to the home page if none has been set.
    /// </summary>
    public IThriveopediaPage SelectedPage
    {
        get => selectedPage ?? homePage;
        set
        {
            // Hide the last page and show the new page
            selectedPage?.Hide();

            selectedPage = value;
            selectedPage.Show();

            UpdateNavigationWithSelectedPage();
        }
    }

    /// <summary>
    ///   Details for the game currently in progress. Null if opened from the main menu. When set, initialises all
    ///   pages requiring an active game.
    /// </summary>
    public GameProperties? CurrentGame
    {
        get => currentGame;
        set
        {
            // We only want to initialise these pages once per game
            if (value == currentGame)
                return;

            currentGame = value;

            // Add all pages associated with a game in progress
            if (currentGame != null)
            {
                AddPage("CurrentWorld");
                AddPage("PatchMap");
                AddPage("EvolutionaryTree");
            }

            // Notify all pages of the new game properties
            foreach (var page in allPages.Keys)
                page.CurrentGame = currentGame;
        }
    }

    public override void _Ready()
    {
        backButton = GetNode<TextureButton>(BackButtonPath);
        forwardButton = GetNode<TextureButton>(ForwardButtonPath);
        pageContainer = GetNode<MarginContainer>(PageContainerPath);
        pageTreeContainer = GetNode<PanelContainer>(PageTreeContainerPath);
        pageTreeContainerAnim = GetNode<AnimationPlayer>(PageTreeContainerAnimPath);
        pageTitle = GetNode<Label>(PageTitlePath);
        viewOnlineButton = GetNode<Button>(ViewOnlineButtonPath);
        pageTree = GetNode<Tree>(PageTreePath);

        // Create and hide a blank root to avoid home being used as the root
        pageTree.CreateItem();
        pageTree.HideRoot = true;

        // Keep a special reference to the home page
        homePage = GetNode<ThriveopediaHomePage>(HomePagePath);
        allPages.Add(homePage, CreateTreeItem(homePage, null));

        // Add all pages not associated with a game in progress
        AddPage("Museum");

        pageHistory.Push(homePage);
        SelectedPage = homePage;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        ThriveopediaManager.ReportActiveThriveopedia(this);
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        ThriveopediaManager.RemoveActiveThriveopedia(this);
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationVisibilityChanged && Visible)
        {
            if (!hasGeneratedWiki)
            {
                AddPage("WikiRoot");
                foreach (var page in ThriveopediaWikiPage.GenerateAllWikiPages())
                    AddPage(page.Name, page);

                hasGeneratedWiki = true;
            }

            foreach (var page in allPages.Keys)
                page.OnThriveopediaOpened();
        }
    }

    /// <summary>
    ///   Opens the Thriveopedia from the main menu.
    /// </summary>
    public void OpenFromMainMenu()
    {
        // Shouldn't do anything if already open
        if (Visible)
            return;

        Show();
    }

    /// <summary>
    ///   Opens the Thriveopedia from a particular game.
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

    /// <summary>
    ///   Opens an existing Thriveopedia page and adds it to the page history.
    /// </summary>
    /// <param name="pageName">The name of the page</param>
    public void ChangePage(string pageName)
    {
        // By default, assume we're navigating to this page normally
        ChangePage(pageName, true, true);
    }

    public Species? GetActiveSpeciesData(uint speciesId)
    {
        if (CurrentGame == null)
        {
            PrintErrorAboutCurrentGame();
            return null;
        }

        CurrentGame.GameWorld.TryGetSpecies(speciesId, out var species);
        return species;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (BackButtonPath != null)
            {
                BackButtonPath.Dispose();
                ForwardButtonPath.Dispose();
                PageContainerPath.Dispose();
                PageTreeContainerPath.Dispose();
                PageTreeContainerAnimPath.Dispose();
                PageTitlePath.Dispose();
                PageTreePath.Dispose();
                HomePagePath.Dispose();
                ViewOnlineButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Updates navigation elements in the Thriveopedia UI to reflect a newly selected page.
    /// </summary>
    private void UpdateNavigationWithSelectedPage()
    {
        // The home page is always the first in the history, so ignore it
        backButton.Disabled = pageHistory.Count <= 1;
        forwardButton.Disabled = pageFuture.Count == 0;

        pageTitle.Text = SelectedPage.TranslatedPageName;
        SelectInTreeWithoutEvent(SelectedPage);

        viewOnlineButton.Visible = SelectedPage is ThriveopediaWikiPage;
    }

    /// <summary>
    ///   Gets an existing page by name, or null if no page exists with that name.
    /// </summary>
    /// <param name="name">The name of the desired page</param>
    /// <returns>The Thriveopedia page with the given name</returns>
    private IThriveopediaPage GetPage(string name)
    {
        var page = allPages.Keys.FirstOrDefault(p => p.PageName == name);

        if (page == null)
            throw new InvalidOperationException($"No page with name {name} found");

        return page;
    }

    /// <summary>
    ///   Adds a page to the Thriveopedia
    /// </summary>
    /// <param name="name">The name of the page</param>
    /// <param name="page">
    ///   Pre-configured page if scene filename does not match the page name or the page needs extra adjustment
    /// </param>
    private void AddPage(string name, IThriveopediaPage? page = null)
    {
        if (allPages.Keys.Any(p => p.PageName == name))
            throw new InvalidOperationException($"Attempted to add duplicate page with name {name}");

        if (page == null)
        {
            var scene = GD.Load<PackedScene>($"res://src/thriveopedia/pages/Thriveopedia{name}Page.tscn");
            page = scene.Instantiate<IThriveopediaPage>();

            if (page == null)
            {
                GD.PrintErr($"Failed to load Thriveopedia page {name} due to scene instantiate failure");
                return;
            }
        }

        if (page.ParentPageName != null && allPages.Keys.All(p => p.PageName != page.ParentPageName))
            throw new InvalidOperationException($"Attempted to add page with name {name} before parent was added");

        page.PageNode.Connect(ThriveopediaPage.SignalName.OnSceneChanged,
            new Callable(this, nameof(HandleSceneChanged)));
        pageContainer.AddChild(page.PageNode);

        var treeItem = CreateTreeItem(page, page.ParentPageName);
        treeItem.Collapsed = page.StartsCollapsed;
        allPages.Add(page, treeItem);

        page.Hide();
    }

    /// <summary>
    ///   Creates an item in the page tree associated with a page.
    /// </summary>
    /// <param name="page">The page the item will link to</param>
    /// <param name="parentName">The name of the page's parent if applicable</param>
    /// <returns>An item in the page tree which links to the given page</returns>
    private TreeItem CreateTreeItem(IThriveopediaPage page, string? parentName = null)
    {
        var pageInTree = pageTree.CreateItem(parentName != null ? allPages[GetPage(parentName)] : null);

        UpdatePageInTree(pageInTree, page);

        return pageInTree;
    }

    private void UpdatePageInTree(TreeItem item, IThriveopediaPage page)
    {
        // Godot doesn't appear to have a left margin for text in items, so add some manual padding
        item.SetText(0, "  " + page.TranslatedPageName);
    }

    /// <summary>
    ///   Selects an item in the page tree without initiating an item selected signal.
    /// </summary>
    /// <param name="page">The page to select</param>
    private void SelectInTreeWithoutEvent(IThriveopediaPage page)
    {
        // Block signals during selection to prevent running code twice, then reattach them
        pageTree.SetBlockSignals(true);
        allPages[page].Select(0);
        pageTree.SetBlockSignals(false);
    }

    private void OnPageSelectedFromPageTree()
    {
        var selected = pageTree.GetSelected();
        var name = allPages.First(p => p.Value == selected).Key.PageName;
        ChangePage(name);
    }

    /// <summary>
    ///   Opens an existing Thriveopedia page, optionally adding it to the page history.
    /// </summary>
    /// <param name="pageName">The name of the page</param>
    /// <param name="addToHistory">Whether this page should be added to the history</param>
    /// <param name="clearFuture">Whether this operation should clear the page future</param>
    private void ChangePage(string pageName, bool addToHistory, bool clearFuture)
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (pageName == SelectedPage.PageName)
            return;

        var page = GetPage(pageName);

        ExpandParents(page);

        if (addToHistory)
            pageHistory.Push(page);

        if (clearFuture)
            pageFuture.Clear();

        SelectedPage = page;
    }

    /// <summary>
    ///   Expands all parents of this page in the tree so that it's visible.
    /// </summary>
    private void ExpandParents(IThriveopediaPage page)
    {
        var parent = allPages.FirstOrDefault(p => p.Key.PageName == page.ParentPageName);

        if (parent.Key == null)
            return;

        parent.Value.Collapsed = false;
        ExpandParents(parent.Key);
    }

    private void OnCollapseTreePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        treeCollapsed = !treeCollapsed;

        if (treeCollapsed)
        {
            pageTreeContainerAnim.Play("Collapse");
        }
        else
        {
            pageTreeContainerAnim.Play("Expand");
        }
    }

    private void OnTreeCollapseStateChanged()
    {
        foreach (var page in allPages.Keys)
        {
            page.OnNavigationPanelSizeChanged(pageTreeContainer.Visible);
        }
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

    private void OnSearchUpdated(string newText)
    {
        pageTree.Clear();
        pageTree.CreateItem();

        // Need to copy to new list as loop modifies the list
        var pageList = allPages.Keys.ToList();
        foreach (var page in pageList)
        {
            var children = GetAllChildren(page);

            if (page.TranslatedPageName.ToLower().Contains(newText.ToLower()) ||
                children.Any(c => c.TranslatedPageName.ToLower().Contains(newText.ToLower())))
            {
                // We can assume a page is always before its children in the list of all pages
                allPages[page] = CreateTreeItem(page, page.ParentPageName);
            }
        }
    }

    /// <summary>
    ///   Recursively gets all descendants of this page in the page tree.
    /// </summary>
    private IEnumerable<IThriveopediaPage> GetAllChildren(IThriveopediaPage page)
    {
        var directChildren = allPages.Keys.Where(p => p.ParentPageName == page.PageName);

        foreach (var directChild in directChildren)
        {
            yield return directChild;

            foreach (var descendant in GetAllChildren(directChild))
            {
                yield return descendant;
            }
        }
    }

    private void OnViewOnlinePressed()
    {
        if (SelectedPage is ThriveopediaWikiPage wikiPage)
        {
            OS.ShellOpen(wikiPage.Url);
        }
    }

    private void HandleSceneChanged()
    {
        EmitSignal(SignalName.OnSceneChanged);
    }

    private void OnClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        Exit();
    }

    private void Exit()
    {
        EmitSignal(SignalName.OnThriveopediaClosed);
    }

    private void OnTranslationsChanged()
    {
        foreach (var page in allPages)
        {
            UpdatePageInTree(page.Value, page.Key);
        }
    }

    private void PrintErrorAboutCurrentGame()
    {
        GD.PrintErr("Thriveopedia doesn't have current game data set yet, but it was already needed");
    }
}
