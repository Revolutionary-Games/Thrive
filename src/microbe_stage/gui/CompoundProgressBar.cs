using System;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;

/// <summary>
///   A progress bar that shows a compound icon, its name and current value in a stylized way. Used by
///   <see cref="CompoundPanels"/>.
/// </summary>
public partial class CompoundProgressBar : Control
{
    private readonly StringBuilder stringBuilder = new();

    private readonly NodePath valueReference = new("value");

    private readonly NodePath minSizeXReference = new("custom_minimum_size:x");

    private readonly StringName normalStyleBoxName = new("normal");

#pragma warning disable CA2213
    [Export]
    private TextureRect? icon;

    [Export]
    private ProgressBar progressBar = null!;

    [Export]
    private Label nameLabel = null!;

    [Export]
    private Label amountLabel = null!;

    [Export]
    private StyleBoxFlat amountCompactTheme = null!;

    private StyleBoxFlat? fillStyleBox;

    private Texture2D? queuedIcon;
#pragma warning restore CA2213

    private bool compact;
    private BarMode mode;

    private LocalizedString? displayedName;

    private float maxValue = 1;
    private float currentValue;

    private string? unitSuffix;
    private bool displayPercent;
    private bool round;

    private bool playingFlashAnimation;
    private double flashAnimationTimer;

    private Color fillColour = new(0.6f, 0.6f, 0.6f, 1);

    public enum BarMode
    {
        Normal,
        ShowOnlyCurrentValue,
        DisableBarShowCurrent,
    }

    /// <summary>
    ///   Name to show on the bar. This is not an export variable as this type isn't supported in Godot. If that is
    ///   needed then it wouldn't be the worst to use a string here and rely on the label translating its value.
    /// </summary>
    public LocalizedString? DisplayedName
    {
        get => displayedName;
        set
        {
            displayedName = value;

            if (fillStyleBox != null)
                UpdateName();
        }
    }

    [Export]
    public float MaxValue
    {
        get => maxValue;
        set
        {
            if (Math.Abs(maxValue - value) < MathUtils.EPSILON)
                return;

            if (maxValue <= 0)
            {
                GD.PrintErr("Cannot set bar max value to 0 or below");
                return;
            }

            maxValue = value;

            if (fillStyleBox != null)
                UpdateValue();
        }
    }

    [Export]
    public float CurrentValue
    {
        get => currentValue;
        set
        {
            if (Math.Abs(currentValue - value) < MathUtils.EPSILON)
                return;

            currentValue = value;

            if (fillStyleBox != null)
                UpdateValue();
        }
    }

    [Export]
    public string? UnitSuffix
    {
        get => unitSuffix;
        set
        {
            if (unitSuffix == value)
                return;

            unitSuffix = value;

            if (fillStyleBox != null)
                UpdateValue();
        }
    }

    [Export]
    public bool DisplayAsPercent
    {
        get => displayPercent;
        set
        {
            if (displayPercent == value)
                return;

            displayPercent = value;

            if (fillStyleBox != null)
                UpdateValue();
        }
    }

    [Export]
    public bool RoundToWholeNumber
    {
        get => round;
        set
        {
            if (round == value)
                return;

            round = value;

            if (fillStyleBox != null)
                UpdateValue();
        }
    }

    [Export]
    public Color FillColour
    {
        get => fillColour;
        set
        {
            fillColour = value;
            UpdateColour();
        }
    }

    [Export]
    public Texture2D? Icon
    {
        get => icon != null ? icon.Texture : queuedIcon;
        set
        {
            if (icon != null)
            {
                icon.Texture = value;
            }
            else
            {
                queuedIcon = value;
            }
        }
    }

    [Export]
    public BarMode Mode
    {
        get => mode;
        set
        {
            if (mode == value)
                return;

            mode = value;

            if (fillStyleBox != null)
                UpdateValue();
        }
    }

    [Export]
    public bool Compact
    {
        get => compact;
        set
        {
            if (compact == value)
                return;

            compact = value;
            ApplyCompactMode(true);
        }
    }

    /// <summary>
    ///   If true, the bar value is slerped to make it animate smoother
    /// </summary>
    [Export]
    public bool SmoothBarValue { get; set; } = true;

    /// <summary>
    ///   Affects the minimum width of this control. Applies only on ready or when animating size.
    /// </summary>
    [Export]
    public bool Narrow { get; set; }

    private double FlashAnimationSlerpFactor => flashAnimationTimer <= Constants.HUD_BAR_FLASH_DURATION * 0.5f ?
        flashAnimationTimer * 2 :
        1 - (flashAnimationTimer - Constants.HUD_BAR_FLASH_DURATION * 0.5f) * 2;

    public static CompoundProgressBar Create(PackedScene scene, CompoundDefinition compound, float initialValue,
        float maxValue)
    {
        var bar = scene.Instantiate<CompoundProgressBar>();

        bar.SetupFromCompound(compound);

        bar.UpdateValue(initialValue, maxValue);
        return bar;
    }

    public static CompoundProgressBar Create(PackedScene scene, Texture2D icon, LocalizedString barName,
        float initialValue, float maxValue)
    {
        var bar = scene.Instantiate<CompoundProgressBar>();

        bar.Icon = icon;
        bar.DisplayedName = barName;

        bar.UpdateValue(initialValue, maxValue);
        return bar;
    }

    /// <summary>
    ///   Creates a bar that shows the value as percentage. Automatically sets max value to 100.
    /// </summary>
    /// <returns>The setup bar instance</returns>
    public static CompoundProgressBar CreatePercentageDisplay(PackedScene scene, CompoundDefinition compound,
        float initialValue, bool round)
    {
        var bar = scene.Instantiate<CompoundProgressBar>();

        bar.SetupFromCompound(compound);

        bar.Mode = BarMode.ShowOnlyCurrentValue;
        bar.DisplayAsPercent = true;
        bar.RoundToWholeNumber = round;

        bar.UpdateValue(initialValue, 100);

        return bar;
    }

    public static CompoundProgressBar CreateSimpleCompound(PackedScene scene, CompoundDefinition compound,
        float initialValue, string? unit)
    {
        unit ??= compound.Unit;

        var bar = scene.Instantiate<CompoundProgressBar>();

        bar.SetupFromCompound(compound);

        bar.Mode = BarMode.ShowOnlyCurrentValue;
        bar.CurrentValue = initialValue;
        bar.UnitSuffix = unit;

        return bar;
    }

    public static CompoundProgressBar CreateSimpleWithUnit(PackedScene scene, CompoundDefinition compound,
        float initialValue, string unit)
    {
        var bar = scene.Instantiate<CompoundProgressBar>();

        bar.SetupFromCompound(compound);

        bar.Mode = BarMode.DisableBarShowCurrent;
        bar.UnitSuffix = unit;
        bar.CurrentValue = initialValue;

        return bar;
    }

    public static CompoundProgressBar CreateSimpleWithUnit(PackedScene scene, Texture2D icon, LocalizedString barName,
        float initialValue, string unit)
    {
        var bar = scene.Instantiate<CompoundProgressBar>();

        bar.Icon = icon;
        bar.DisplayedName = barName;

        bar.Mode = BarMode.DisableBarShowCurrent;
        bar.CurrentValue = initialValue;
        bar.UnitSuffix = unit;

        return bar;
    }

    public override void _Ready()
    {
        if (icon == null)
            throw new Exception("Icon not set through Godot editor");

        if (queuedIcon != null)
        {
            icon.Texture = queuedIcon;
        }

        // TODO: check that this fetches per scene instances properly
        fillStyleBox = (StyleBoxFlat)progressBar.GetThemeStylebox("fill");

        UpdateName();
        UpdateValue();
        UpdateColour();

        if (compact)
        {
            ApplyCompactMode(false);
        }
        else
        {
            if (Narrow)
            {
                CustomMinimumSize = new Vector2(Constants.COMPOUND_BAR_NARROW_NORMAL_WIDTH, CustomMinimumSize.Y);
            }
            else
            {
                CustomMinimumSize = new Vector2(Constants.COMPOUND_BAR_NORMAL_WIDTH, CustomMinimumSize.Y);
            }
        }

        SetProcess(playingFlashAnimation);
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;

        // Animation is reset when entering the tree
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;

        // Cancel animation if exiting the tree while playing one
        if (playingFlashAnimation)
        {
            playingFlashAnimation = false;

            if (icon != null)
                icon.SelfModulate = Colors.White;

            UpdateColour();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!playingFlashAnimation)
        {
            GD.PrintErr("Flash animation not playing, but we got _Process call...");
            SetProcess(false);
            return;
        }

        flashAnimationTimer += delta;

        if (flashAnimationTimer > Constants.HUD_BAR_FLASH_DURATION)
        {
            playingFlashAnimation = false;
            flashAnimationTimer = 0;
            SetProcess(false);

            if (icon != null)
                icon.SelfModulate = Colors.White;
        }
        else if (icon != null)
        {
            icon.SelfModulate = Colors.White.Lerp(Colors.Red, (float)FlashAnimationSlerpFactor);
        }

        UpdateColour();
    }

    /// <summary>
    ///   Updates both value and max value in a more efficient way than setting the properties individually.
    /// </summary>
    /// <param name="value">Current value to set</param>
    /// <param name="max">Max value the bar can have</param>
    public void UpdateValue(float value, float max)
    {
        if (Math.Abs(maxValue - max) + Math.Abs(value - currentValue) < MathUtils.EPSILON)
            return;

        currentValue = value;
        maxValue = max;

        if (fillStyleBox != null)
            UpdateValue();
    }

    /// <summary>
    ///   Sets the bar value to show a fraction (range 0-1) as percentage (range 0-100). Bar max value should already
    ///   be set to 100 before calling this.
    /// </summary>
    /// <param name="fraction">The fraction to convert to percentage for display</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValueAsPercentageFromFraction(float fraction)
    {
        CurrentValue = fraction * 100;
    }

    public void SetupFromCompound(CompoundDefinition compound)
    {
        Icon = compound.LoadedIcon ?? throw new Exception("Compound type has no icon loaded");
        FillColour = compound.BarColour;
        DisplayedName = new LocalizedString(compound.GetUntranslatedName());
    }

    /// <summary>
    ///   Plays a flashing animation on this bar if not playing already
    /// </summary>
    public void Flash()
    {
        if (playingFlashAnimation)
            return;

        playingFlashAnimation = true;
        flashAnimationTimer = 0;
        SetProcess(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            valueReference.Dispose();
            minSizeXReference.Dispose();
        }

        normalStyleBoxName.Dispose();
        base.Dispose(disposing);
    }

    private void UpdateName()
    {
        if (DisplayedName != null)
            nameLabel.Text = DisplayedName.ToString();
    }

    private void UpdateValue()
    {
        stringBuilder.Clear();

        switch (Mode)
        {
            case BarMode.Normal:
            {
                if (round)
                {
                    StringUtils.SlashSeparatedNumbersFormat(Math.Round(currentValue), Math.Round(maxValue),
                        stringBuilder);
                }
                else
                {
                    StringUtils.SlashSeparatedNumbersFormat(currentValue, maxValue, stringBuilder);
                }

                if (unitSuffix != null || displayPercent)
                {
                    // TODO: determine if this "hack" actually results in good looking text (formatting a blank as
                    // the value to give some extra spacing here)
                    if (displayPercent)
                    {
                        stringBuilder.Append(Localization.Translate("PERCENTAGE_VALUE").FormatSafe(" "));
                    }
                    else
                    {
                        stringBuilder.Append(Localization.Translate("VALUE_WITH_UNIT")
                            .FormatSafe(" ", unitSuffix));
                    }
                }

                SetBarValue();

                break;
            }

            case BarMode.ShowOnlyCurrentValue:
            {
                FormatOnlyCurrentValue();

                SetBarValue();

                break;
            }

            case BarMode.DisableBarShowCurrent:
            {
                FormatOnlyCurrentValue();

                progressBar.Value = 0;

                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        amountLabel.Text = stringBuilder.ToString();
    }

    private void SetBarValue()
    {
        // Scale the progress to range 0-1 which is what is used in the bar
        var target = Math.Clamp(currentValue / maxValue, 0, 1);

        if (SmoothBarValue)
        {
            var tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(progressBar, valueReference, target,
                Constants.COMPOUND_BAR_VALUE_ANIMATION_TIME);
        }
        else
        {
            progressBar.Value = target;
        }
    }

    private void FormatOnlyCurrentValue()
    {
        if (round)
        {
            StringUtils.ThreeDigitFormat(Math.Round(currentValue), stringBuilder);
        }
        else
        {
            StringUtils.ThreeDigitFormat(currentValue, stringBuilder);
        }

        if (unitSuffix != null || displayPercent)
        {
            // TODO: avoid this temporary string somehow
            var temp = stringBuilder.ToString();
            stringBuilder.Clear();

            if (displayPercent)
            {
                stringBuilder.Append(Localization.Translate("PERCENTAGE_VALUE").FormatSafe(temp));
            }
            else
            {
                stringBuilder.Append(Localization.Translate("VALUE_WITH_UNIT").FormatSafe(temp, unitSuffix));
            }
        }
    }

    private void UpdateColour()
    {
        if (fillStyleBox == null)
            return;

        if (!playingFlashAnimation)
        {
            fillStyleBox.BgColor = fillColour;
        }
        else
        {
            fillStyleBox.BgColor = fillColour.Lerp(Colors.Red, (float)FlashAnimationSlerpFactor);
        }
    }

    private void ApplyCompactMode(bool playAnimation)
    {
        // Do not play an animation if not inside the tree as this is still being setup
        if (playAnimation && !IsInsideTree())
            playAnimation = false;

        if (playAnimation)
        {
            var tween = CreateTween();

            if (compact)
            {
                tween.TweenProperty(this, minSizeXReference,
                    Narrow ? Constants.COMPOUND_BAR_NARROW_COMPACT_WIDTH : Constants.COMPOUND_BAR_COMPACT_WIDTH, 0.3);

                amountLabel.AddThemeStyleboxOverride(normalStyleBoxName, amountCompactTheme);
                nameLabel.Hide();
            }
            else
            {
                tween.TweenProperty(this, minSizeXReference,
                    Narrow ? Constants.COMPOUND_BAR_NARROW_NORMAL_WIDTH : Constants.COMPOUND_BAR_NORMAL_WIDTH, 0.3);

                amountLabel.RemoveThemeStyleboxOverride(normalStyleBoxName);
                nameLabel.Show();
            }
        }
        else
        {
            if (compact)
            {
                CustomMinimumSize =
                    new Vector2(
                        Narrow ? Constants.COMPOUND_BAR_NARROW_COMPACT_WIDTH : Constants.COMPOUND_BAR_COMPACT_WIDTH,
                        CustomMinimumSize.Y);

                amountLabel.AddThemeStyleboxOverride(normalStyleBoxName, amountCompactTheme);
                nameLabel.Hide();
            }
            else
            {
                CustomMinimumSize =
                    new Vector2(
                        Narrow ? Constants.COMPOUND_BAR_NARROW_NORMAL_WIDTH : Constants.COMPOUND_BAR_NORMAL_WIDTH,
                        CustomMinimumSize.Y);

                amountLabel.RemoveThemeStyleboxOverride(normalStyleBoxName);
                nameLabel.Show();
            }
        }
    }

    private void OnTranslationsChanged()
    {
        UpdateName();
        UpdateValue();
    }
}
