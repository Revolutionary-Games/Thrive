using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    ///   This is where presets are stored after a colour picker exited scene tree.
    ///   <remarks>
    ///     <para>
    ///       The Key is the group name; <br />
    ///       The Value is the preset storage.
    ///     </para>
    ///   </remarks>
    /// </summary>
    private static readonly Dictionary<string, PresetGroupStorage> PresetsStorage = new();

    /// <summary>
    ///   This is the local storage of its preset children
    ///   so that we don't need to call GetChildren() when deleting one.
    /// </summary>
    private readonly List<TweakedColourPickerPreset> presets = new();

#pragma warning disable CA2213
    private HSlider sliderROrH = null!;
    private HSlider sliderGOrS = null!;
    private HSlider sliderBOrV = null!;

    // Alpha has all 3 elements to fully hide it
    private HSlider sliderA = null!;
    private Control labelA = null!;
    private Control spinboxA = null!;
    private Button pickerButton = null!;
#pragma warning restore CA2213

    // TODO:
    private ColorModeType mode = ColorModeType.Rgb;

    [Export]
    public string PresetGroup { get; private set; } = "default";

    /// <summary>
    ///   Change the picker's colour mode.
    /// </summary>
    [Export]
    public new ColorModeType ColorMode
    {
        get => base.ColorMode;
        set
        {
            mode = value;
            if (value != ColorModeType.Raw)
                ValidateRgbColor();

            base.ColorMode = mode;
            UpdateTooltips();
        }
    }

    // TODO: fix this with Godot 4
    /// <summary>
    ///   Hide Godot property PresetsEnabled to avoid unexpected changes
    ///   which may cause the hidden native buttons reappear.
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        // Hide replaced native controls. Can't delete them because it will crash Godot.
        // TODO: fix this: this no longer has the same child layout
        var baseControl = GetChild(0, true).GetChild(0);

        // Hide RGB/HSV/RAW buttons
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
        spinboxA = baseControl.GetChild(4).GetChild(0).GetChild<Control>(14);
        pickerButton = baseControl.GetChild(1).GetChild<Button>(0);

        baseControl.GetChild(2).GetChild<MenuButton>(3).GetPopup().IndexPressed += HideAlphaSlider;
        HideAlphaSlider(1);

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

        if (mode == ColorModeType.Hsv)
        {
            sliderROrH.TooltipText = Localization.Translate("COLOUR_PICKER_H_TOOLTIP");
            sliderGOrS.TooltipText = Localization.Translate("COLOUR_PICKER_S_TOOLTIP");
            sliderBOrV.TooltipText = Localization.Translate("COLOUR_PICKER_V_TOOLTIP");
        }
        else
        {
            if (mode == ColorModeType.Rgb)
            {
                sliderROrH.TooltipText = Localization.Translate("COLOUR_PICKER_R_TOOLTIP");
                sliderGOrS.TooltipText = Localization.Translate("COLOUR_PICKER_G_TOOLTIP");
                sliderBOrV.TooltipText = Localization.Translate("COLOUR_PICKER_B_TOOLTIP");
            }

            // TODO: Add text for OKHSL mode
        }

        sliderA.TooltipText = Localization.Translate("COLOUR_PICKER_A_TOOLTIP");
    }

    private void HideAlphaSlider(long dummyIndex)
    {
        sliderA.Hide();
        labelA.Hide();
        spinboxA.Hide();
    }

    private partial class TweakedColourPickerPreset : ColorRect
    {
        private readonly TweakedColourPicker owner;

        public TweakedColourPickerPreset(TweakedColourPicker owner, Color colour)
        {
            this.owner = owner;
            Color = colour;

            // Init the GUI part of the ColorRect
            OffsetTop = OffsetBottom = OffsetLeft = OffsetRight = 6.0f;
            CustomMinimumSize = new Vector2(20, 20);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            UpdateTooltip();
        }

        public override void _Notification(int what)
        {
            if (what == NotificationTranslationChanged)
                UpdateTooltip();

            base._Notification(what);
        }

        private void UpdateTooltip()
        {
            TooltipText = Localization.Translate("COLOUR_PICKER_PRESET_TOOLTIP")
                .FormatSafe(Color.IsRaw() ? "argb(" + Color + ")" : "#" + Color.ToHtml());
        }
    }

    private class PresetGroupStorage : IEnumerable<Color>
    {
        private readonly List<Color> colours;

        public PresetGroupStorage(IEnumerable<Color> colours)
        {
            this.colours = colours.ToList();
        }

        public IEnumerator<Color> GetEnumerator()
        {
            return colours.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
