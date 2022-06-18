using System.Globalization;
using Godot;
using Newtonsoft.Json;

public class CellStatIndicator : HBoxContainer
{
    private Label? descriptionLabel;
    private Label? valueLabel;
    private TextureRect? changeIndicator;

    private Texture increaseIcon = null!;
    private Texture decreaseIcon = null!;
    private Texture questionIcon = null!;

    private string description = "unset";
    private string format = string.Empty;
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

    [Export]
    public string Format
    {
        get => format;
        set
        {
            format = value;
            UpdateValue();
        }
    }

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

    public void ResetInitialValue(float value)
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
            Value.ToString(CultureInfo.CurrentCulture) : string.Format(CultureInfo.CurrentCulture, Format, Value);
    }
}
