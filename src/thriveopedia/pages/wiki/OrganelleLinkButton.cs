using System;
using Godot;

/// <summary>
///   Button which links to an organelle Thriveopedia page.
/// </summary>
public class OrganelleLinkButton : VBoxContainer
{
    private Button button = null!;
    private Label label = null!;

    public OrganelleDefinition Organelle { get; set; } = null!;

    public Action OpenLink { get; set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        button = GetNode<Button>("Button");
        label = GetNode<Label>("Label");

        button.Icon = GD.Load<Texture>(Organelle.IconPath);
        label.Text = Organelle.Name;
    }

    public void OnPressed() => OpenLink();
}
