﻿using Godot;

// Instances are created only through code
// ReSharper disable once Godot.MissingParameterlessConstructor
/// <summary>
///   Label showing info about a hovered entity in the game with the cursor
/// </summary>
public partial class InspectedEntityLabel : HBoxContainer
{
#pragma warning disable CA2213 // Disposable fields should be disposed
    private Label textLabel;
    private Label? descriptionLabel;

    private Texture2D? icon;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public InspectedEntityLabel(string text, Texture2D? icon = null)
    {
        this.icon = icon;

        textLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        SetText(text);
    }

    public override void _Ready()
    {
        if (icon != null)
            AddChild(GUICommon.Instance.CreateIcon(icon, 20, 20));

        AddChild(textLabel);

        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>
    ///   Sets the main text (left-side).
    /// </summary>
    public void SetText(string text)
    {
        textLabel.Text = text;
    }

    /// <summary>
    ///   Sets the description of the main text (right-side).
    /// </summary>
    public void SetDescription(string description)
    {
        EnsureDescriptionLabelExist();
        descriptionLabel!.Text = description;
    }

    public void SetDescriptionColor(Color color)
    {
        EnsureDescriptionLabelExist();
        descriptionLabel!.Modulate = color;
    }

    private void EnsureDescriptionLabelExist()
    {
        if (descriptionLabel != null)
            return;

        descriptionLabel = new Label();
        AddChild(descriptionLabel);
    }
}
