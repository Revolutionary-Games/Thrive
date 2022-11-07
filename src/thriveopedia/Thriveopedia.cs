﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Central store of game information for the player. Acts like a browser. Can be opened in-game and from the main
///   menu. Some pages relating to a game in progress are only available in-game.
/// </summary>
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
    public NodePath PageTitlePath = null!;

    [Export]
    public NodePath PageTreePath = null!;

    [Export]
    public NodePath HomePagePath = null!;

    private TextureButton backButton = null!;
    private TextureButton forwardButton = null!;
    private MarginContainer pageContainer = null!;
    private PanelContainer pageTreeContainer = null!;
    private Label pageTitle = null!;
    private Tree pageTree = null!;

    /// <summary>
    ///   The home page for the Thriveopedia. Keep a special reference so we can return to it easily.
    /// </summary>
    private ThriveopediaHomePage homePage = null!;

    /// <summary>
    ///   Details for the game currently in progress. Null if opened from the main menu.
    /// </summary>
    private GameProperties? currentGame;

    /// <summary>
    ///   The currently open Thriveopedia page.
    /// </summary>
    private ThriveopediaPage? selectedPage;

    /// <summary>
    ///   All Thriveopedia pages and their associated items in the page tree.
    /// </summary>
    private Dictionary<ThriveopediaPage, TreeItem> allPages = new();

    /// <summary>
    ///   Page history for backwards navigation.
    /// </summary>
    private Stack<ThriveopediaPage> pageHistory = new();

    /// <summary>
    ///   Page future for forwards navigation (if the player has already gone backwards).
    /// </summary>
    private Stack<ThriveopediaPage> pageFuture = new();

    [Signal]
    public delegate void OnThriveopediaClosed();

    [Signal]
    public delegate void OnSceneChanged();

    /// <summary>
    ///   The currently open Thriveopedia page. Defaults to the home page if none has been set.
    /// </summary>
    public ThriveopediaPage SelectedPage
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
                AddPage("PatchMap", "CurrentWorld");
                AddPage("EvolutionaryTree", "CurrentWorld");
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
        pageTitle = GetNode<Label>(PageTitlePath);
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

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationVisibilityChanged && Visible)
        {
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
    }

    /// <summary>
    ///   Gets an existing page by name, or null if no page exists with that name.
    /// </summary>
    /// <param name="name">The name of the desired page</param>
    /// <returns>The Thriveopedia page with the given name</returns>
    private ThriveopediaPage GetPage(string name)
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
    /// <param name="parentName">The name of the page's parent if applicable</param>
    private void AddPage(string name, string? parentName = null)
    {
        // Avoid adding duplicate pages
        if (allPages.Keys.Any(p => p.PageName == name))
            throw new InvalidOperationException($"Attempted to add duplicate page with name {name}");

        // For now, load by direct reference to the Godot scene. Could be generalised in future.
        var scene = GD.Load<PackedScene>($"res://src/thriveopedia/pages/Thriveopedia{name}Page.tscn");
        var page = (ThriveopediaPage)scene.Instance();
        page.Connect(nameof(ThriveopediaPage.OnSceneChanged), this, nameof(HandleSceneChanged));
        pageContainer.AddChild(page);
        allPages.Add(page, CreateTreeItem(page, parentName));
        page.Hide();
    }

    /// <summary>
    ///   Creates an item in the page tree associated with a page.
    /// </summary>
    /// <param name="page">The page the item will link to</param>
    /// <param name="parentName">The name of the page's parent if applicable</param>
    /// <returns>An item in the page tree which links to the given page</returns>
    private TreeItem CreateTreeItem(ThriveopediaPage page, string? parentName = null)
    {
        var pageInTree = pageTree.CreateItem(parentName != null ? allPages[GetPage(parentName)] : null);

        // Set the name of the tree item so we can reference it later
        pageInTree.SetMeta("name", page.PageName);

        // Godot doesn't appear to have a left margin for text in items, so add some manual padding
        pageInTree.SetText(0, "  " + page.TranslatedPageName);

        return pageInTree;
    }

    /// <summary>
    ///   Selects an item in the page tree without initiating an item selected signal.
    /// </summary>
    /// <param name="page">The page to select</param>
    private void SelectInTreeWithoutEvent(ThriveopediaPage page)
    {
        // Block signals during selection to prevent running code twice, then reattach them
        pageTree.SetBlockSignals(true);
        allPages[page].Select(0);
        pageTree.SetBlockSignals(false);
    }

    private void OnPageSelectedFromPageTree()
    {
        var name = (string)pageTree.GetSelected().GetMeta("name");
        ChangePage(name);
    }

    /// <summary>
    ///    Opens an existing Thriveopedia page, optionally adding it to the page history.
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

        if (addToHistory)
            pageHistory.Push(page);

        if (clearFuture)
            pageFuture.Clear();

        SelectedPage = page;
    }

    private void OnCollapseTreePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        pageTreeContainer.Visible = !pageTreeContainer.Visible;
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

    private void HandleSceneChanged()
    {
        EmitSignal(nameof(OnSceneChanged));
    }

    private void OnClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        Exit();
    }

    private void Exit()
    {
        EmitSignal(nameof(OnThriveopediaClosed));
    }
}
