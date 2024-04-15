using System;
using Godot;

/// <summary>
///   Used to display a modifier info as UI element on a selection menu tooltip
///   (eg. +10 Osmoregulation Cost)
/// </summary>
public partial class ModifierInfoLabel : HBoxContainer
{
#pragma warning disable CA2213
    private Label? nameLabel;
    private Label? valueLabel;
    private TextureRect? icon;

    private LabelSettings modifierNameColor = null!;
    private LabelSettings modifierValueColor = null!;

    private LabelSettings? originalModifier;
    private LabelSettings? positiveModifierColour;
    private LabelSettings? negativeModifierColour;

    private Texture2D? iconTexture;
#pragma warning restore CA2213

    private string displayName = string.Empty;
    private string modifierValue = string.Empty;

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
    public LabelSettings ModifierNameColor
    {
        get => modifierNameColor;
        set
        {
            // These null checks were added here to guard against bug only in exported game
            if (value == null!)
                throw new ArgumentNullException();

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
            if (value == null!)
                throw new ArgumentNullException();

            modifierValueColor = value;

            // This is called before the cleanup below as this will stop using the data to be deleted
            UpdateValue();

            // Reset dependant colour data
            originalModifier = null;
            positiveModifierColour?.Dispose();
            positiveModifierColour = null;
            negativeModifierColour?.Dispose();
            negativeModifierColour = null;
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
        // These null checks were added here to guard against bug only in exported game
        if (modifierNameColor == null)
        {
            throw new InvalidOperationException("Modifier info label doesn't have name font set, at: " +
                GetPath());
        }

        if (modifierValueColor == null)
        {
            throw new InvalidOperationException("Modifier info label doesn't have value font set, at: " +
                GetPath());
        }

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

        // Note that this method doesn't set things through ModifierValueColor as that would be detected as a new
        // default colour. This is a bit of a mess as this code wasn't re-architected for Godot 4 but instead hacked
        // to still fit the original pattern by creating variants of label settings.

        if (value > 0)
        {
            if (inverted)
            {
                negativeModifierColour ??= originalModifier.CloneWithDifferentColour(new Color(1, 0.3f, 0.3f));
                modifierValueColor = negativeModifierColour;
            }
            else
            {
                positiveModifierColour ??= originalModifier.CloneWithDifferentColour(new Color(0, 1, 0));
                modifierValueColor = positiveModifierColour;
            }
        }
        else if (value == 0)
        {
            if (originalModifier != null)
                modifierValueColor = originalModifier;
        }
        else
        {
            if (inverted)
            {
                positiveModifierColour ??= originalModifier.CloneWithDifferentColour(new Color(0, 1, 0));
                modifierValueColor = positiveModifierColour;
            }
            else
            {
                negativeModifierColour ??= originalModifier.CloneWithDifferentColour(new Color(1, 0.3f, 0.3f));
                modifierValueColor = negativeModifierColour;
            }
        }

        UpdateValue();
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
