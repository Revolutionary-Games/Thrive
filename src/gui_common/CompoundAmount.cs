﻿using System;
using System.Globalization;
using Godot;

/// <summary>
///   Shows a compound amount along with an icon
/// </summary>
public partial class CompoundAmount : HBoxContainer
{
    private readonly StringName colourParameterName = new("font_color");

#pragma warning disable CA2213
    private Label? amountLabel;
    private Control? amountSuffixSpacer;
    private Label? amountSuffixLabel;
    private TextureRect? icon;
    private Label? extraDescriptionLabel;
#pragma warning restore CA2213

    private Compound compound = Compound.Invalid;
    private CompoundDefinition? compoundDefinition;

    private int decimals = 3;
    private float amount = float.NegativeInfinity;
    private string? amountSuffix;
    private bool prefixPositiveWithPlus;
    private bool usePercentageDisplay;
    private Colour valueColour = Colour.White;
    private LocalizedString? extraValueDescription;

    public enum Colour
    {
        White,
        Red,
    }

    /// <summary>
    ///   The compound to show
    /// </summary>
    public Compound Compound
    {
        set
        {
            if (value == Compound.Invalid)
                throw new ArgumentNullException();

            if (compound == value)
                return;

            compound = value;
            compoundDefinition = SimulationParameters.GetCompound(compound);

            if (icon != null)
            {
                UpdateIcon();
                UpdateTooltip();
            }
        }
    }

    /// <summary>
    ///   The compound amount to show
    /// </summary>
    public float Amount
    {
        get => amount;
        set
        {
            if (amount == value)
                return;

            amount = value;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   If not null this suffix is added to the amount (with a space added between the values)
    /// </summary>
    public string? AmountSuffix
    {
        get => amountSuffix;
        set
        {
            if (amountSuffix == value)
                return;

            amountSuffix = value;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   Number of decimals to show in the amount
    /// </summary>
    public int Decimals
    {
        get => decimals;
        set
        {
            if (decimals == value)
                return;

            decimals = value;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   If true positive (>= 0) amounts are prefixed with a plus.
    /// </summary>
    public bool PrefixPositiveWithPlus
    {
        get => prefixPositiveWithPlus;
        set
        {
            if (prefixPositiveWithPlus == value)
                return;

            prefixPositiveWithPlus = value;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   If true  numbers are shown as percentages.
    /// </summary>
    public bool UsePercentageDisplay
    {
        get => usePercentageDisplay;
        set
        {
            if (usePercentageDisplay == value)
                return;

            usePercentageDisplay = value;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   Colour of the amount display
    /// </summary>
    public Colour ValueColour
    {
        get => valueColour;
        set
        {
            if (valueColour == value)
                return;

            valueColour = value;
            UpdateColour();
        }
    }

    /// <summary>
    ///   When set, extra information can be shown after the compound value
    /// </summary>
    public LocalizedString? ExtraValueDescription
    {
        get => extraValueDescription;
        set
        {
            if (extraValueDescription != null && extraValueDescription.Equals(value))
                return;

            extraValueDescription = value;
            UpdateExtraDescription();
        }
    }

    /// <summary>
    ///   If set to false, the unit will not be shown for the amount
    /// </summary>
    public bool ShowUnit { get; set; } = true;

    public override void _Ready()
    {
        base._Ready();

        if (compound == Compound.Invalid || compoundDefinition == null)
            throw new InvalidOperationException($"Need to set {nameof(Compound)}");

        UpdateLabel();
        UpdateIcon();
        UpdateTooltip();

        // Only apply non-default colour here. If it is later changed, it is then applied
        if (ValueColour != Colour.White)
            UpdateColour();

        if (extraValueDescription != null)
            UpdateExtraDescription();
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

    /// <summary>
    ///   Call if the state of the external extra text to show has changed without the property being re-assigned
    /// </summary>
    public void OnExtraTextChangedExternally()
    {
        if (ExtraValueDescription != null)
            UpdateExtraDescription();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            colourParameterName.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateLabel()
    {
        if (amountLabel == null)
        {
            amountLabel = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            AddChild(amountLabel);
        }

        string numberPart;
        if (!string.IsNullOrEmpty(compoundDefinition!.Unit) && ShowUnit)
        {
            numberPart = Localization.Translate("VALUE_WITH_UNIT")
                .FormatSafe(Math.Round(amount), compoundDefinition.Unit);
        }
        else if (UsePercentageDisplay)
        {
            numberPart = Localization.Translate("PERCENTAGE_VALUE").FormatSafe(Math.Round(amount * 100, 1));
        }
        else
        {
            numberPart = Math.Round(amount, decimals).ToString(CultureInfo.CurrentCulture);
        }

        if (amountSuffix != null)
        {
            if (amountSuffixSpacer == null)
            {
                amountSuffixSpacer = new Control
                {
                    CustomMinimumSize = new Vector2(1, 0),
                };

                AddChild(amountSuffixSpacer);
            }

            if (amountSuffixLabel == null)
            {
                amountSuffixLabel = new Label
                {
                    VerticalAlignment = VerticalAlignment.Center,

                    // TODO: find a solution for turning on word wrapping
                    // AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    // CustomMinimumSize = new Vector2(30, 0),
                };

                AddChild(amountSuffixLabel);
            }

            amountSuffixLabel.Text = amountSuffix;
        }
        else
        {
            if (amountSuffixLabel != null)
            {
                amountSuffixLabel.QueueFree();
                amountSuffixLabel = null;
            }

            if (amountSuffixSpacer != null)
            {
                amountSuffixSpacer.QueueFree();
                amountSuffixSpacer = null;
            }
        }

        amountLabel.Text = PrefixPositiveWithPlus ?
            StringUtils.FormatPositiveWithLeadingPlus(numberPart, amount) :
            numberPart;
    }

    private void UpdateColour()
    {
        if (amountLabel == null)
            return;

        Color color;

        switch (ValueColour)
        {
            case Colour.White:
                color = new Color(1.0f, 1.0f, 1.0f);
                break;
            case Colour.Red:
                color = new Color(1.0f, 0.3f, 0.3f);
                break;
            default:
                throw new Exception("unhandled colour");
        }

        amountLabel.AddThemeColorOverride(colourParameterName, color);
    }

    private void UpdateExtraDescription()
    {
        if (ExtraValueDescription == null)
        {
            extraDescriptionLabel?.QueueFree();
            extraDescriptionLabel = null;
        }
        else
        {
            if (extraDescriptionLabel == null)
            {
                extraDescriptionLabel = new Label
                {
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    CustomMinimumSize = new Vector2(20, 0),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };

                AddChild(extraDescriptionLabel);
            }

            // Make sure the extra description label is last child
            MoveChild(extraDescriptionLabel, -1);

            extraDescriptionLabel.Text = ExtraValueDescription.ToString();
        }
    }

    private void UpdateIcon()
    {
        icon?.Free();

        icon = GUICommon.Instance.CreateCompoundIcon(compoundDefinition!.InternalName);
        icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        AddChild(icon);

        if (extraDescriptionLabel != null)
            UpdateExtraDescription();
    }

    private void UpdateTooltip()
    {
        if (icon != null)
            icon.TooltipText = compoundDefinition!.Name;
    }

    private void OnTranslationsChanged()
    {
        UpdateTooltip();
        UpdateLabel();
        UpdateExtraDescription();
    }
}
