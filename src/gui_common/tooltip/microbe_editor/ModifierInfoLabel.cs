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

    [Export]
    public string ModifierName
    {
        get => modifierName;
        set
        {
            modifierName = value;
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

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("Name");
        valueLabel = GetNode<Label>("Value");
        icon = GetNode<TextureRect>("Icon");

        UpdateName();
        UpdateValue();
        UpdateIcon();
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            return;

        if (string.IsNullOrEmpty(ModifierName))
        {
            modifierName = nameLabel.Text;
        }
        else
        {
            nameLabel.Text = modifierName;
        }
    }

    private void UpdateValue()
    {
        if (valueLabel == null)
            return;

        if (string.IsNullOrEmpty(ModifierValue))
        {
            modifierValue = valueLabel.Text;
        }
        else
        {
            valueLabel.Text = modifierValue;
        }

        if (ModifierValueColor == new Color(0, 0, 0, 0))
        {
            valueLabel.GetColor("font_color");
        }
        else
        {
            valueLabel.AddColorOverride("font_color", modifierValueColor);
        }
    }

    private void UpdateIcon()
    {
        if (icon == null)
            return;

        if (ModifierIcon == null)
        {
            iconTexture = icon.Texture;
        }
        else
        {
            icon.Texture = iconTexture;
        }
    }
}
