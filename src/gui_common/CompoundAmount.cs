using System;
using System.Globalization;
using Godot;

/// <summary>
///   Shows a compound amount along with an icon
/// </summary>
public partial class CompoundAmount : HBoxContainer
{
#pragma warning disable CA2213
    private Label? amountLabel;
    private TextureRect? icon;
#pragma warning restore CA2213

    private Compound? compound;

    private int decimals = 3;
    private float amount = float.NegativeInfinity;
    private bool prefixPositiveWithPlus;
    private bool usePercentageDisplay;
    private Colour valueColour = Colour.White;

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
            if (value == null)
                throw new ArgumentNullException();

            if (compound == value)
                return;

            compound = value;

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

    public override void _Ready()
    {
        base._Ready();

        if (compound == null)
            throw new InvalidOperationException($"Need to set {nameof(Compound)}");

        UpdateLabel();
        UpdateIcon();
        UpdateTooltip();

        // Only apply non-default colour here. If it is later changed, it is then applied
        if (ValueColour != Colour.White)
            UpdateColour();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateTooltip();

            UpdateLabel();
        }
    }

    private void UpdateLabel()
    {
        if (amountLabel == null)
        {
            amountLabel = new Label();
            AddChild(amountLabel);
        }

        string numberPart;
        if (!string.IsNullOrEmpty(compound!.Unit))
        {
            numberPart = TranslationServer.Translate("VALUE_WITH_UNIT").FormatSafe(Math.Round(amount), compound.Unit);
        }
        else if (UsePercentageDisplay)
        {
            numberPart = TranslationServer.Translate("PERCENTAGE_VALUE").FormatSafe(Math.Round(amount * 100, 1));
        }
        else
        {
            numberPart = Math.Round(amount, decimals).ToString(CultureInfo.CurrentCulture);
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

        amountLabel.AddThemeColorOverride("font_color", color);
    }

    private void UpdateIcon()
    {
        icon?.Free();

        icon = GUICommon.Instance.CreateCompoundIcon(compound!.InternalName);
        AddChild(icon);
    }

    private void UpdateTooltip()
    {
        if (icon != null)
            icon.TooltipText = compound!.Name;
    }
}
