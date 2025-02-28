using System;
using System.Globalization;
using Godot;

/// <summary>
///   Shows modifiers on a statistic and what the effective value is
/// </summary>
public partial class StatModifierToolTip : Control, ICustomToolTip
{
#pragma warning disable CA2213
    [Export]
    [ExportCategory("Internal")]
    private Label valueLabel = null!;

    [Export]
    private CustomRichTextLabel descriptionLabel = null!;

    [Export]
    private CustomRichTextLabel extraDescriptionLabel = null!;

    [Export]
    private Control extraDescriptionSeparator = null!;

    [Export]
    private GridContainer statsContainer = null!;
#pragma warning restore CA2213

    private string? displayName;

    private double displayedValue;

    private int decimalsToShow = 2;

    private bool formatAsPercentage;

    [Export]
    [ExportCategory("Configuration")]
    public string DisplayName
    {
        get => displayName ?? "StatModifierToolTip_unset";
        set
        {
            displayName = value;
        }
    }

    [Export]
    public string? Description { get; set; }

    [Export]
    public string? ExtraDescription { get; set; }

    [Export]
    public double DisplayedValue
    {
        get => displayedValue;
        set
        {
            if (Math.Abs(displayedValue - value) < MathUtils.EPSILON)
                return;

            displayedValue = value;
            UpdateValueDisplay();
        }
    }

    [Export]
    public int ShownDecimals
    {
        get => decimalsToShow;
        set
        {
            if (decimalsToShow == value)
                return;

            decimalsToShow = value;
            UpdateValueDisplay();
        }
    }

    [Export]
    public bool ShowAsPercentage
    {
        get => formatAsPercentage;
        set
        {
            if (formatAsPercentage == value)
                return;

            formatAsPercentage = value;
            UpdateValueDisplay();
        }
    }

    [Export]
    public float DisplayDelay { get; set; }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.ControlBottomRightCorner;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    public bool HideOnMouseAction { get; set; }

    public Control ToolTipNode => this;

    public override void _Ready()
    {
        base._Ready();

        descriptionLabel.ExtendedBbcode = Description;

        UpdateValueDisplay();

        if (string.IsNullOrEmpty(ExtraDescription))
        {
            extraDescriptionSeparator.Visible = false;
            extraDescriptionLabel.Visible = false;
        }
        else
        {
            extraDescriptionLabel.ExtendedBbcode = ExtraDescription;
        }
    }

    private void UpdateValueDisplay()
    {
        var value = displayedValue;

        if (formatAsPercentage)
            value *= 100;

        value = Math.Round(value, decimalsToShow);

        if (formatAsPercentage)
        {
            var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");

            valueLabel.Text = percentageFormat.FormatSafe(value);
        }
        else
        {
            valueLabel.Text = value.ToString(CultureInfo.CurrentCulture);
        }
    }
}
