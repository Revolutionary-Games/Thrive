using System;
using System.Globalization;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Shows cell stats (e.g. Storage: 2.1, Hp: 50, etc) for the organism statistics display.
///   Also functions as a comparison for old value with a new one, indicated with an up/down icon.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class CellStatsIndicator : HBoxContainer
{
#pragma warning disable CA2213
    /// <summary>
    ///   The icon to be displayed when <see cref="Value"/> hasn't been assigned to or is <see langword="NaN"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Uses question mark icon by default (if this isn't set).
    ///   </para>
    /// </remarks>
    [Export]
    public Texture2D? InvalidIcon;

    [Export]
    public Texture2D? Icon;

    [Export]
    public NodePath? ValuePath;

    private Label? descriptionLabel;
    private Label? valueLabel;
    private TextureRect? changeIndicator;
    private TextureRect? iconRect;

    private Texture2D blankIcon = null!;
    private Texture2D increaseIcon = null!;
    private Texture2D decreaseIcon = null!;
#pragma warning restore CA2213

    private string description = "unset";
    private string? format;
    private float value;

    [JsonProperty]
    private float? initialValue;

    /// <summary>
    ///   The minimum rect size of the change indicator in normal state (the up/down arrow).
    /// </summary>
    [Export]
    public Vector2 ChangeIndicatorSize { get; set; } = new(10, 10);

    /// <summary>
    ///   The minimum rect size of the change indicator in invalid state.
    /// </summary>
    [Export]
    public Vector2 InvalidIndicatorSize { get; set; } = new(10, 10);

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
        valueLabel = GetNode<Label>(ValuePath);
        changeIndicator = GetNode<TextureRect>("Indicator");
        iconRect = GetNode<TextureRect>("Icon");

        InvalidIcon ??= GD.Load<Texture2D>("res://assets/textures/gui/bevel/helpButton.png");

        blankIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/blankStat.png");
        increaseIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/decrease.png");

        iconRect.Texture = Icon;

        // Hide the icon displayer when no icon to save on some horizontal space
        if (Icon == null)
            iconRect.Visible = false;

        UpdateChangeIndicator();
        UpdateDescription();
        UpdateValue();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
    }

    public void ResetInitialValue()
    {
        initialValue = null;
        UpdateValue();
    }

    /// <summary>
    ///   Displays a multipart value on this indicator. Use when setting <see cref="Value"/> is not enough
    /// </summary>
    /// <param name="formattedValue">The text to display on this indicator</param>
    /// <param name="rawValueForComparison">Value used to compare this indicator value against</param>
    public void SetMultipartValue(string formattedValue, float rawValueForComparison)
    {
        if (!float.IsNaN(rawValueForComparison))
            initialValue ??= rawValueForComparison;

        value = rawValueForComparison;

        if (valueLabel == null)
            throw new InvalidOperationException("This method can only be called after adding to the scene tree");

        UpdateChangeIndicator();

        valueLabel.Text = formattedValue;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ValuePath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateChangeIndicator()
    {
        if (changeIndicator == null)
            return;

        if (initialValue.HasValue && !float.IsNaN(initialValue.Value) && !float.IsNaN(Value))
        {
            changeIndicator.CustomMinimumSize = ChangeIndicatorSize;
            if (Value > initialValue)
            {
                changeIndicator.Texture = increaseIcon;
            }
            else if (Value < initialValue)
            {
                changeIndicator.Texture = decreaseIcon;
            }
            else
            {
                changeIndicator.Texture = blankIcon;
            }

            changeIndicator.Visible = true;
        }
        else
        {
            changeIndicator.CustomMinimumSize = InvalidIndicatorSize;
            changeIndicator.Texture = InvalidIcon;
            changeIndicator.Visible = true;
        }
    }

    private void UpdateDescription()
    {
        if (descriptionLabel == null)
            return;

        descriptionLabel.Text = Localization.Translate(Description);
    }

    private void UpdateValue()
    {
        if (valueLabel == null || changeIndicator == null)
            return;

        UpdateChangeIndicator();

        valueLabel.Text = string.IsNullOrEmpty(Format) ?
            Value.ToString(CultureInfo.CurrentCulture) :
            Format!.FormatSafe(Value);
    }

    private void OnTranslationsChanged()
    {
        UpdateDescription();
        UpdateValue();
    }
}
