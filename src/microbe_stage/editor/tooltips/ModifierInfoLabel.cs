using System;
using Godot;

/// <summary>
///   Used to display a modifier info as UI element on a selection menu tooltip
///   (eg. +10 Osmoregulation Cost)
/// </summary>
public partial class ModifierInfoLabel : HBoxContainer
{
    private readonly Lazy<LabelSettings> positiveModifierColour;
    private readonly Lazy<LabelSettings> negativeModifierColour;

#pragma warning disable CA2213
    private Label? nameLabel;
    private Label? valueLabel;
    private TextureRect? icon;

    private LabelSettings modifierNameColor = null!;
    private LabelSettings modifierValueColor = null!;
    private LabelSettings? originalModifier;

    private Texture2D? iconTexture;
#pragma warning restore CA2213

    private string displayName = string.Empty;
    private string modifierValue = string.Empty;

    private bool showValue = true;

    public ModifierInfoLabel()
    {
        // This is a bit of a mess due to Godot 4 conversion, but for now this is enough and keeps AdjustValueColor
        // API the same
        positiveModifierColour =
            new Lazy<LabelSettings>(() => originalModifier!.CloneWithDifferentColour(new Color(0, 1, 0)));
        negativeModifierColour =
            new Lazy<LabelSettings>(() => originalModifier!.CloneWithDifferentColour(new Color(1, 0.3f, 0.3f)));
    }

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
    public LabelSettings ModifierNameColor
    {
        get => modifierNameColor;
        set
        {
            modifierNameColor = value;
            UpdateName();
        }
    }

    [Export]
    public LabelSettings ModifierValueColor
    {
        get => modifierValueColor;
        set
        {
            modifierValueColor = value;
            UpdateValue();
        }
    }

    [Export]
    public Texture2D? ModifierIcon
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
        valueLabel = GetNode<Label>("HBoxContainer/Value");
        icon = GetNode<TextureRect>("Icon");

        UpdateName();
        UpdateValue();
        UpdateIcon();
        AdjustValueMinSize(40.0f);
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
        originalModifier ??= ModifierValueColor;

        if (value > 0)
        {
            ModifierValueColor = inverted ? negativeModifierColour.Value : positiveModifierColour.Value;
        }
        else if (value == 0)
        {
            if (originalModifier != null)
                ModifierValueColor = originalModifier;
        }
        else
        {
            ModifierValueColor = inverted ? positiveModifierColour.Value : negativeModifierColour.Value;
        }
    }

    private void AdjustValueMinSize(float size)
    {
        if (valueLabel != null)
            valueLabel.CustomMinimumSize = new Vector2(size, 20.0f);
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            return;

        nameLabel.Text = displayName;
        nameLabel.LabelSettings = modifierNameColor;
    }

    private void UpdateValue()
    {
        if (valueLabel == null)
            return;

        valueLabel.Visible = ShowValue;

        valueLabel.Text = modifierValue;

        valueLabel.LabelSettings = modifierValueColor;
    }

    private void UpdateIcon()
    {
        if (icon == null)
            return;

        icon.Texture = iconTexture;
    }
}
