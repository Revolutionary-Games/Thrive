using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Shows modifiers on a statistic and what the effective value is
/// </summary>
public partial class StatModifierToolTip : Control, ICustomToolTip
{
    private readonly List<(Label Title, Label Value)> shownStats = new();

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

    private LabelSettings? breakdownFont;
#pragma warning restore CA2213

    private string? displayName;

    private double displayedValue;

    private int decimalsToShow = 2;

    private bool formatAsPercentage;
    private string? valueSuffix;

    [Export]
    [ExportCategory("Configuration")]
    public string DisplayName
    {
        get => displayName ?? "StatModifierToolTip_unset";
        set => displayName = value;
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
    public string? ValueSuffix
    {
        get => valueSuffix;
        set
        {
            if (valueSuffix == value)
                return;

            valueSuffix = value;
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

    [Export]
    public LabelSettings? BreakdownFont
    {
        get => breakdownFont;
        set => breakdownFont = value;
    }

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

    /// <summary>
    ///   Shows a breakdown of values divided based on organelle types. Clears any old data that shouldn't be shown
    ///   any more.
    /// </summary>
    /// <param name="itemsAndValues">Data to show</param>
    public void DisplayOrganelleBreakdown(Dictionary<OrganelleDefinition, float> itemsAndValues)
    {
        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");

        int usedIndex = 0;

        // Probably fine to use extra memory here to go in sorted order
        foreach (var pair in itemsAndValues.OrderByDescending(p => p.Value))
        {
            Label title;
            Label value;

            if (usedIndex >= shownStats.Count)
            {
                title = new Label
                {
                    LabelSettings = breakdownFont,
                };

                value = new Label
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    LabelSettings = breakdownFont,
                };

                statsContainer.AddChild(title);
                statsContainer.AddChild(value);
                shownStats.Add((title, value));
            }
            else
            {
                (title, value) = shownStats[usedIndex];
            }

            title.Text = pair.Key.Name;

            double valueToShow = pair.Value;

            if (formatAsPercentage)
                valueToShow *= 100;

            valueToShow = Math.Round(valueToShow, decimalsToShow);

            if (formatAsPercentage)
            {
                value.Text =
                    StringUtils.FormatPositiveWithLeadingPlus(percentageFormat.FormatSafe(valueToShow), pair.Value);
            }
            else
            {
                value.Text =
                    StringUtils.FormatPositiveWithLeadingPlus(valueToShow.ToString(CultureInfo.CurrentCulture),
                        pair.Value);
            }

            ++usedIndex;
        }

        while (shownStats.Count > usedIndex)
        {
            var (title, value) = shownStats[^1];
            title.QueueFree();
            value.QueueFree();

            shownStats.RemoveAt(shownStats.Count - 1);
        }
    }

    private void UpdateValueDisplay()
    {
        var value = displayedValue;

        if (formatAsPercentage)
            value *= 100;

        value = Math.Round(value, decimalsToShow);

        string text;

        if (formatAsPercentage)
        {
            var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");

            text = percentageFormat.FormatSafe(value);
        }
        else
        {
            text = value.ToString(CultureInfo.CurrentCulture);
        }

        if (!string.IsNullOrEmpty(valueSuffix))
            text = $"{text} {valueSuffix}";

        valueLabel.Text = text;
    }
}
