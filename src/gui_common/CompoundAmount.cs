﻿using System;
using System.Globalization;
using Godot;

/// <summary>
///   Shows a compound amount along with an icon
/// </summary>
public class CompoundAmount : HBoxContainer
{
    private Label amountLabel;
    private TextureRect icon;

    private Compound compound;

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
        get => compound;
        set
        {
            if (compound == value)
                return;

            compound = value;

            if (icon != null)
                UpdateIcon();
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
            if (amountLabel != null)
                UpdateColour();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        UpdateLabel();
        UpdateIcon();

        // Only apply non-default colour here. If it is later changed, it is then applied
        if (ValueColour != Colour.White)
            UpdateColour();
    }

    private void UpdateLabel()
    {
        if (amountLabel == null)
        {
            amountLabel = new Label();
            AddChild(amountLabel);
        }

        string numberPart;
        if (UsePercentageDisplay)
        {
            numberPart = Math.Round(amount * 100, 1) + "%";
        }
        else
        {
            numberPart = Math.Round(amount, decimals).ToString(CultureInfo.CurrentCulture);
        }

        if (PrefixPositiveWithPlus && amount >= 0)
        {
            amountLabel.Text = "+" + numberPart;
        }
        else
        {
            amountLabel.Text = numberPart;
        }
    }

    private void UpdateColour()
    {
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

        amountLabel.AddColorOverride("font_color", color);
    }

    private void UpdateIcon()
    {
        icon?.DetachAndFree();

        icon = GUICommon.Instance.CreateCompoundIcon(compound.InternalName);
        AddChild(icon);
    }
}
