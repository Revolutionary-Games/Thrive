﻿using Godot;

/// <summary>
///   Used to display a modifier info as UI element on a selection menu tooltip
///   (eg. +10 Osmoregulation Cost)
/// </summary>
public class ModifierInfoLabel : HBoxContainer
{
    private Label nameLabel;
    private Label valueLabel;
    private TextureRect icon;

    private string displayName;
    private string modifierValue;
    private Color modifierValueColor;
    private Texture iconTexture;

    private bool showValue = true;

    [Export]
    public string DisplayName
    {
        get => displayName;
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    [Export]
    public string ModifierValue
    {
        get => modifierValue;
        set
        {
            modifierValue = value;
            UpdateValue();
        }
    }

    [Export]
    public Color ModifierValueColor
    {
        get => modifierValueColor;
        set
        {
            modifierValueColor = value;
            UpdateValue();
        }
    }

    [Export]
    public Texture ModifierIcon
    {
        get => iconTexture;
        set
        {
            iconTexture = value;
            UpdateIcon();
        }
    }

    /// <summary>
    ///   Useful for modifier labels that doesn't require a value to be shown.
    /// </summary>
    [Export]
    public bool ShowValue
    {
        get => showValue;
        set
        {
            showValue = value;
            UpdateValue();
        }
    }

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("Name");
        valueLabel = GetNode<Label>("Value");
        icon = GetNode<TextureRect>("Icon");

        UpdateName();
        UpdateValue();
        UpdateIcon();
    }

    /// <summary>
    ///   Helper method for setting the color of the value text to either green,
    ///   white or red based on the magnitude of the given numerical value.
    /// </summary>
    /// <param name="value">Positive numbers = green, negative numbers = red.</param>
    /// <param name="inverted">
    ///   <para>
    ///     Inverts the color choice (e.g. Red color for positive numbers).
    ///     This is useful for modifiers like Osmoregulation Cost (A disadvantage
    ///     for cells at increased value, thus the red color imply that).
    ///   </para>
    /// </param>
    public void AdjustValueColor(float value, bool inverted = false)
    {
        if (value > 0)
        {
            ModifierValueColor = inverted ? new Color(1, 0.3f, 0.3f) : new Color(0, 1, 0);
        }
        else if (value == 0)
        {
            ModifierValueColor = new Color(1, 1, 1);
        }
        else
        {
            ModifierValueColor = inverted ? new Color(0, 1, 0) : new Color(1, 0.3f, 0.3f);
        }
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            return;

        nameLabel.Text = displayName;
    }

    private void UpdateValue()
    {
        if (valueLabel == null)
            return;

        valueLabel.Visible = ShowValue;

        valueLabel.Text = modifierValue;

        valueLabel.AddColorOverride("font_color", modifierValueColor);
    }

    private void UpdateIcon()
    {
        if (icon == null)
            return;

        icon.Texture = iconTexture;
    }
}
