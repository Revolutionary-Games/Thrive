using Godot;
using System;

public abstract class ThriveopediaPage : PanelContainer
{
    [Export]
    public NodePath PageTitlePath = null!;

    private GameProperties? currentGame = null!;

    public abstract string PageName { get; }

    public abstract string TranslatedPageName { get; }

    public Action<string, string> AddPageAsChild = null!;

    public Action<string> OpenPage = null!;

    public TreeItem? PageTreeItem;

    public GameProperties? CurrentGame
    {
        get => currentGame;
        set
        {
            currentGame = value;

            UpdateCurrentWorldDetails();
        }
    }

    private Label pageTitle = null!;

    public override void _Ready()
    {
        base._Ready();

        pageTitle = GetNode<Label>(PageTitlePath);
        pageTitle.Text = TranslatedPageName;
    }

    public abstract void UpdateCurrentWorldDetails();
}