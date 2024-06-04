using System;
using Godot;

/// <summary>
///   Button which links to a Thriveopedia page.
/// </summary>
public partial class PageLinkButton : VBoxContainer
{
    [Export]
    public NodePath? ButtonPath;

    [Export]
    public NodePath LabelPath = null!;

#pragma warning disable CA2213
    private Button button = null!;
    private Label label = null!;
#pragma warning restore CA2213

    public string IconPath { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public Action OpenLink { get; set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        button = GetNode<Button>(ButtonPath);
        label = GetNode<Label>(LabelPath);

        button.Icon = GD.Load<Texture2D>(IconPath);
        label.Text = DisplayName;
    }

    public void OnPressed()
    {
        OpenLink();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ButtonPath != null)
            {
                ButtonPath.Dispose();
                LabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
