using System.Globalization;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Shows cell stats (e.g. Storage: 2.1, Hp: 50, etc) for the organism statistics display.
///   Also functions as a comparison for old value with a new one, indicated with an up/down icon.
/// </summary>
public class CellStatsIndicator : HBoxContainer
{
#pragma warning disable CA2213
    private Label? descriptionLabel;
    private Label? valueLabel;
    private TextureRect? changeIndicator;

    private Texture increaseIcon = null!;
    private Texture decreaseIcon = null!;
    private Texture questionIcon = null!;
#pragma warning restore CA2213

    private string description = "unset";
    private string? format;
    private float value;

    [JsonProperty]
    private float? initialValue;

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

    /// <summary>
    ///   Displays the value in a formatted string, use this to show units (e.g. {0} m/s).
    /// </summary>
    [Export]
    public string? Format
    {
        get => format;
        set
        {
            format = value;
            UpdateValue();
        }
    }

    /// <summary>
    ///   First assignment sets the initial value, subsequent assignment sets the other values to be compared with.
    /// </summary>
    [Export]
    public float Value
    {
        get => value;
        set
        {
            if (!float.IsNaN(value))
                initialValue ??= value;
            this.value = value;
            UpdateValue();
        }
    }

    public override void _Ready()
    {
        descriptionLabel = GetNode<Label>("Description");
        valueLabel = GetNode<Label>("Value");
        changeIndicator = GetNode<TextureRect>("Indicator");

        increaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/decrease.png");
        questionIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/helpButton.png");

        UpdateDescription();
        UpdateValue();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateDescription();
            UpdateValue();
        }
    }

    public void ResetInitialValue()
    {
        initialValue = null;
        UpdateValue();
    }

    private void UpdateDescription()
    {
        if (descriptionLabel == null)
            return;

        descriptionLabel.Text = TranslationServer.Translate(Description);
    }

    private void UpdateValue()
    {
        if (valueLabel == null || changeIndicator == null)
            return;

        if (initialValue.HasValue && !float.IsNaN(initialValue.Value) && !float.IsNaN(Value))
        {
            changeIndicator.Texture = Value > initialValue ? increaseIcon : decreaseIcon;
            changeIndicator.Visible = Value != initialValue;
        }
        else
        {
            changeIndicator.Texture = questionIcon;
            changeIndicator.Visible = true;
        }

        valueLabel.Text = string.IsNullOrEmpty(Format) ?
            Value.ToString(CultureInfo.CurrentCulture) :
            Format!.FormatSafe(Value);
    }
}
