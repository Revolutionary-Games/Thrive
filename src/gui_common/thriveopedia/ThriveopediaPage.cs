using Godot;
using System;

public abstract class ThriveopediaPage : PanelContainer
{
    [Export]
    public NodePath PageTitlePath = null!;

    private GameProperties? currentGame = null!;

    public abstract string PageName { get; }

    public Action<string> OpenPage = null!;

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
        pageTitle.Text = TranslationServer.Translate(PageName);
    }

    public abstract void UpdateCurrentWorldDetails();
}