using System.Globalization;
using Godot;

/// <summary>
///   Displays a stat of a cell type; also displays an icon and a description label.
/// </summary>
public partial class CellTypeStatLabel : HBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private Texture2D icon = null!;

    [Export]
    private Label descriptionLabel = null!;

    [Export]
    private Label valueLabel = null!;

    [Export]
    private TextureRect iconRect = null!;
#pragma warning restore CA2213

    private string description = "unset";

    private float value;

    [Export]
    public string Description
    {
        get => description;
        set
        {
            description = value;
            UpdateDescription();
        }
    }

    public float Value
    {
        get => value;
        set
        {
            this.value = value;
            UpdateValue();
        }
    }

    public override void _Ready()
    {
        iconRect.Texture = icon;
    }

    private void UpdateDescription()
    {
        descriptionLabel.Text = Localization.Translate(Description);
    }

    private void UpdateValue()
    {
        valueLabel.Text = Value.ToString(CultureInfo.CurrentCulture);
    }
}
