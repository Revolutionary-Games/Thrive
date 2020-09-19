using Godot;

/// <summary>
///   Used to display a modifier info as UI element on a selection menu tooltip
///   (eg. +10 Osmoregulation Cost)
/// </summary>
public class ModifierInfoLabel : HBoxContainer
{
    private Label nameLabel;
    private Label valueLabel;
    private TextureRect icon;

    private string modifierName;
    private string modifierValue;
    private Color modifierValueColor;
    private Texture iconTexture;

    public string ModifierName
    {
        get => modifierName;
        set
        {
            modifierName = value;
            UpdateName();
        }
    }

    public string ModifierValue
    {
        get => modifierValue;
        set
        {
            modifierValue = value;
            UpdateValue();
        }
    }

    public Color ModifierValueColor
    {
        get => valueLabel.GetColor("font_color");
        set
        {
            modifierValueColor = value;
            UpdateValue();
        }
    }

    public Texture ModifierIcon
    {
        get => iconTexture;
        set
        {
            iconTexture = value;
            UpdateIcon();
        }
    }

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("Name");
        valueLabel = GetNode<Label>("Value");
        icon = GetNode<TextureRect>("Icon");
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            return;

        nameLabel.Text = modifierName;
    }

    private void UpdateValue()
    {
        if (valueLabel == null)
            return;

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
