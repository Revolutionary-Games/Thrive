using Godot;

/// <summary>
///   Tweaked colour picker allows for tooltip texts translations and has a customized theming.
/// </summary>
/// <remarks>
///   <para>
///     TODO: add support for the new OKHSL mode in Godot
///   </para>
/// </remarks>
public partial class TweakedColourPicker : ColorPicker
{
#pragma warning disable CA2213
    private HSlider sliderROrH = null!;
    private HSlider sliderGOrS = null!;
    private HSlider sliderBOrV = null!;

    // Alpha has all 3 elements to fully hide it
    private HSlider sliderA = null!;
    private Control labelA = null!;
    private Control spinBoxA = null!;
    private Button pickerButton = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Currently our customized presets are (almost) completely removed.
    /// </summary>
    public string PresetGroup { get; private set; } = "default";

    public override void _Ready()
    {
        base._Ready();

        var baseControl = GetChild(0, true).GetChild(0);

        // Hide RGB/HSV/RAW buttons. Can't delete them because it will crash Godot.
        baseControl.GetChild(2).GetChild<Control>(0).Hide();
        baseControl.GetChild(2).GetChild<Control>(1).Hide();
        baseControl.GetChild(2).GetChild<Control>(2).Hide();

        // Get controls
        // Sliders are now also for HSL (OKHSL)
        sliderROrH = baseControl.GetChild(4).GetChild(0).GetChild<HSlider>(4);
        sliderGOrS = baseControl.GetChild(4).GetChild(0).GetChild<HSlider>(7);
        sliderBOrV = baseControl.GetChild(4).GetChild(0).GetChild<HSlider>(10);
        sliderA = baseControl.GetChild(4).GetChild(0).GetChild<HSlider>(13);
        labelA = baseControl.GetChild(4).GetChild(0).GetChild<Control>(12);
        spinBoxA = baseControl.GetChild(4).GetChild(0).GetChild<Control>(14);
        pickerButton = baseControl.GetChild(1).GetChild<Button>(0);

        baseControl.GetChild(2).GetChild<MenuButton>(3).GetPopup().Connect(PopupMenu.SignalName.IndexPressed,
            new Callable(this, nameof(HideAlphaSlider)));
        baseControl.GetChild(2).GetChild<MenuButton>(3).GetPopup().Connect(PopupMenu.SignalName.IndexPressed,
            new Callable(this, nameof(UpdateTooltips)));
        HideAlphaSlider();

        // Disable RAW option in a dropdown menu
        baseControl.GetChild(2).GetChild<MenuButton>(3).GetPopup().SetItemDisabled(2, true);

        // Disable value bar scroll with the mouse, as the colour pickers are often in scrollable containers and
        // this would otherwise be problematic. Perhaps in the future we should have this be configurable with an
        // export property?

        sliderROrH.Scrollable = false;
        sliderGOrS.Scrollable = false;
        sliderBOrV.Scrollable = false;

        UpdateTooltips();
    }

    /// <summary>
    ///   Change the TweakedColourPicker's current colour.
    ///   <remarks>
    ///     <para>
    ///       This emits the color_changed signal, used by the custom colour picker preset when it's selected so
    ///       that other elements can be notified of the newly selected colour.
    ///     </para>
    ///   </remarks>
    /// </summary>
    public void SetColour(Color colour)
    {
        if (Color == colour)
            return;

        Color = colour;
        EmitSignal("color_changed", colour);
    }

    /// <summary>
    ///   When return from raw mode make sure the three values are within RGB standard. (Maximum value 1)
    /// </summary>
    private void ValidateRgbColor()
    {
        var colour = Color;

        if (colour.R > 1)
            colour.R = 1;
        if (colour.G > 1)
            colour.G = 1;
        if (colour.B > 1)
            colour.B = 1;

        SetColour(colour);
    }

    private void UpdateTooltips()
    {
        pickerButton.TooltipText = Localization.Translate("COLOUR_PICKER_PICK_COLOUR");

        if (ColorMode == ColorModeType.Hsv)
        {
            sliderROrH.TooltipText = Localization.Translate("COLOUR_PICKER_H_TOOLTIP");
            sliderGOrS.TooltipText = Localization.Translate("COLOUR_PICKER_S_TOOLTIP");
            sliderBOrV.TooltipText = Localization.Translate("COLOUR_PICKER_V_TOOLTIP");
        }
        else
        {
            if (ColorMode == ColorModeType.Rgb)
            {
                sliderROrH.TooltipText = Localization.Translate("COLOUR_PICKER_R_TOOLTIP");
                sliderGOrS.TooltipText = Localization.Translate("COLOUR_PICKER_G_TOOLTIP");
                sliderBOrV.TooltipText = Localization.Translate("COLOUR_PICKER_B_TOOLTIP");
            }

            // TODO: Add tooltips for OKHSL mode
        }

        sliderA.TooltipText = Localization.Translate("COLOUR_PICKER_A_TOOLTIP");
    }

    private void HideAlphaSlider()
    {
        sliderA.Hide();
        labelA.Hide();
        spinBoxA.Hide();
    }

    private void DummyTranslations()
    {
        // Dummy translations for later use (for reimplementing presets)
        Localization.Translate("COLOUR_PICKER_PRESET_TOOLTIP");
        Localization.Translate("COLOUR_PICKER_ADD_PRESET");
        Localization.Translate("COLOUR_PICKER_PRESET_TOOLTIP");

        // TODO: check if these value translations are still used by Godot 4:
        Localization.Translate("HSV");
        Localization.Translate("RAW");

        // TODO: check if these are ever going to be useful:
        Localization.Translate("COLOUR_PICKER_HSV_BUTTON_TOOLTIP");
        Localization.Translate("COLOUR_PICKER_RAW_BUTTON_TOOLTIP");
    }
}
