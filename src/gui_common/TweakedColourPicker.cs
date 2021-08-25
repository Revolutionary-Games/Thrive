using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Tweaked colour picker allows for tooltip texts translations and has a customized theming.
/// </summary>
public class TweakedColourPicker : ColorPicker
{
    /// <summary>
    ///   This is where presets are stored after a colour picker exited scene tree.
    ///   <remarks>
    ///     The Key is the group name; <br />
    ///     The Value is the preset storage.
    ///   </remarks>
    /// </summary>
    private static readonly Dictionary<string, PresetGroupStorage> PresetsStorage
        = new Dictionary<string, PresetGroupStorage>();

    private readonly List<TweakedColourPickerPreset> presets = new List<TweakedColourPickerPreset>();

    private HSlider sliderROrH;
    private HSlider sliderGOrS;
    private HSlider sliderBOrV;
    private HSlider sliderA;
    private ToolButton pickerButton;
    private CheckButton hsvCheckButton;
    private CheckButton rawCheckButton;
    private LineEdit htmlColourEdit;
    private HSeparator separator;
    private GridContainer presetsContainer;
    private TextureButton addPresetButton;

    private bool hsvButtonEnabled = true;
    private bool rawButtonEnabled = true;
    private bool presetsEnabled = true;
    private bool presetsVisible = true;

    private PresetGroupStorage groupStorage;

    private delegate void AddPresetDelegate(Color colour);

    private delegate void DeletePresetDelegate(Color colour);

    [Export]
    public string PresetGroup { get; private set; } = "default";

    /// <summary>
    ///   Decide if user can toggle HSV CheckButton to switch HSV mode.
    /// </summary>
    [Export]
    public bool HSVButtonEnabled
    {
        get => hsvButtonEnabled;
        set
        {
            hsvButtonEnabled = value;
            UpdateButtonsState();
        }
    }

    /// <summary>
    ///   Decide if user can toggle raw CheckButton to switch raw mode.
    /// </summary>
    [Export]
    public bool RawButtonEnabled
    {
        get => rawButtonEnabled;
        set
        {
            rawButtonEnabled = value;
            UpdateButtonsState();
        }
    }

    /// <summary>
    ///   Change the picker's HSV mode.
    ///   <remarks>
    ///     This is not named HSVMode because this hides a Godot property to avoid breaking custom functions.
    ///   </remarks>
    /// </summary>
    [Export]
    public new bool HsvMode
    {
        get => base.HsvMode;
        set
        {
            if (value && RawMode)
                return;

            base.HsvMode = value;
            UpdateButtonsState();

            // Update HSV sliders' tooltips
            UpdateTooltips();
        }
    }

    /// <summary>
    ///   Change the picker's raw mode.
    /// </summary>
    [Export]
    public new bool RawMode
    {
        get => base.RawMode;
        set
        {
            if (value && HsvMode)
                return;

            base.RawMode = value;
            UpdateButtonsState();
        }
    }

    /// <summary>
    ///   Decide if user can edit the presets.
    ///   <remarks>
    ///     This is not named to PresetsEditable because this also hides a Godot property.
    ///   </remarks>
    /// </summary>
    [Export]
    public new bool PresetsEnabled
    {
        get => presetsEnabled;
        set
        {
            presetsEnabled = value;

            if (addPresetButton == null)
                return;

            addPresetButton.Disabled = !value;
        }
    }

    /// <summary>
    ///   Decide if the presets and the add preset button is visible.
    /// </summary>
    [Export]
    public new bool PresetsVisible
    {
        get => presetsVisible;
        set
        {
            presetsVisible = value;

            if (presetsContainer == null)
                return;

            separator.Visible = value;
            presetsContainer.Visible = value;
            addPresetButton.Visible = value;
        }
    }

    // Have to disable this warning because this hides Godot property Color to emit a signal when changing its color.
#pragma warning disable CA1721
    public new Color Color
#pragma warning restore CA1721
    {
        get => base.Color;
        set
        {
            base.Color = value;
            EmitSignal("color_changed", value);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // Hide replaced native controls. Can't delete them because it will crash Godot.
        var baseControl = GetChild(4);
        baseControl.GetChild<Control>(4).Hide();
        GetChild<Control>(5).Hide();
        GetChild<Control>(6).Hide();
        GetChild<Control>(7).Hide();

        // Get controls
        sliderROrH = baseControl.GetChild(0).GetChild<HSlider>(1);
        sliderGOrS = baseControl.GetChild(1).GetChild<HSlider>(1);
        sliderBOrV = baseControl.GetChild(2).GetChild<HSlider>(1);
        sliderA = baseControl.GetChild(3).GetChild<HSlider>(1);
        pickerButton = GetChild(1).GetChild<ToolButton>(1);
        hsvCheckButton = GetNode<CheckButton>("MarginButtonsContainer/ButtonsContainer/HSVCheckButton");
        rawCheckButton = GetNode<CheckButton>("MarginButtonsContainer/ButtonsContainer/RawCheckButton");
        htmlColourEdit = GetNode<LineEdit>("MarginButtonsContainer/ButtonsContainer/HtmlColourEdit");
        separator = GetNode<HSeparator>("Separator");
        presetsContainer = GetNode<GridContainer>("PresetContainer");
        addPresetButton = GetNode<TextureButton>("PresetButtonContainer/AddPresetButton");

        // Update control state.
        UpdateButtonsState();
        PresetsEnabled = presetsEnabled;
        PresetsVisible = presetsVisible;
        OnColourChanged(Color);

        // Load presets.
        if (PresetsStorage.TryGetValue(PresetGroup, out groupStorage))
        {
            foreach (var colour in groupStorage.Colours)
                GroupAddPreset(colour);
        }
        else
        {
            // Always ensure there is one so then we just modify it instead of having to check.
            groupStorage = new PresetGroupStorage(GetPresets());
            PresetsStorage.Add(PresetGroup, groupStorage);
        }

        // Add current preset handlers to preset group
        groupStorage.AddPresetDelegate += GroupAddPreset;
        groupStorage.ErasePresetDelegate += GroupErasePreset;

        UpdateTooltips();
    }

    public override void _ExitTree()
    {
        groupStorage.AddPresetDelegate -= GroupAddPreset;
        groupStorage.ErasePresetDelegate -= GroupErasePreset;
        base._ExitTree();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            UpdateTooltips();

        base._Notification(what);
    }

    public new void AddPreset(Color colour)
    {
        // Broadcast to all group numbers.
        groupStorage.AddPreset(colour);
    }

    public new void ErasePreset(Color colour)
    {
        groupStorage.ErasePreset(colour);
    }

    private void GroupAddPreset(Color colour)
    {
        // Add preset locally
        var preset = new TweakedColourPickerPreset(this, colour);
        presets.Add(preset);
        presetsContainer.AddChild(preset);

        // Add preset to base class
        base.AddPreset(colour);
    }

    private void GroupErasePreset(Color colour)
    {
        var preset = presets.First(p => p.Color == colour);
        presets.Remove(preset);
        presetsContainer.RemoveChild(preset);
        preset.QueueFree();

        base.ErasePreset(colour);
    }

    private void UpdateTooltips()
    {
        pickerButton.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_PICK_COLOUR");
        addPresetButton.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_ADD_PRESET");
        hsvCheckButton.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_HSV_BUTTON_TOOLTIP");
        rawCheckButton.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_RAW_BUTTON_TOOLTIP");

        if (HsvMode)
        {
            sliderROrH.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_H_TOOLTIP");
            sliderGOrS.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_S_TOOLTIP");
            sliderBOrV.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_V_TOOLTIP");
        }
        else
        {
            sliderROrH.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_R_TOOLTIP");
            sliderGOrS.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_G_TOOLTIP");
            sliderBOrV.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_B_TOOLTIP");
        }

        sliderA.HintTooltip = TranslationServer.Translate("COLOUR_PICKER_A_TOOLTIP");
    }

    private void UpdateButtonsState()
    {
        if (hsvCheckButton == null)
            return;

        hsvCheckButton.Pressed = HsvMode;
        rawCheckButton.Pressed = RawMode;

        hsvCheckButton.Disabled = !hsvButtonEnabled || RawMode;
        rawCheckButton.Disabled = !rawButtonEnabled || HsvMode;
    }

    private void OnAddPresetButtonPressed()
    {
        if (!presetsEnabled)
            return;

        AddPreset(Color);
    }

    private void OnHSVButtonToggled(bool isOn)
    {
        HsvMode = isOn;
    }

    private void OnRawButtonToggled(bool isOn)
    {
        RawMode = isOn;
    }

    private void OnColourChanged(Color colour)
    {
        htmlColourEdit.Text = colour.ToHtml();
    }

    /// <summary>
    ///   Called when (keyboard) entered in HtmlColourEdit or from OnHtmlColourEditFocusExited.
    ///   Set Color when text is valid; reset if not.
    /// </summary>
    /// <param name="colour">Current htmlColourEditor text</param>
    private void OnHtmlColourEditEntered(string colour)
    {
        if (colour.IsValidHtmlColor())
        {
            Color = new Color(colour);
        }
        else
        {
            htmlColourEdit.Text = Color.ToHtml();
        }
    }

    /// <summary>
    ///   Called when focus exited HtmlColourEdit.
    /// </summary>
    private void OnHtmlColourEditFocusExited()
    {
        OnHtmlColourEditEntered(htmlColourEdit.Text);
        htmlColourEdit.Deselect();
    }

    private class TweakedColourPickerPreset : ColorRect
    {
        private readonly TweakedColourPicker owner;

        public TweakedColourPickerPreset(TweakedColourPicker owner, Color colour)
        {
            this.owner = owner;
            Color = colour;
            MarginTop = MarginBottom = MarginLeft = MarginRight = 6.0f;
            RectMinSize = new Vector2(20, 20);
            SizeFlagsHorizontal = (int)SizeFlags.ShrinkEnd;
            SizeFlagsVertical = (int)SizeFlags.ShrinkCenter;
            UpdateTooltip();
        }

        public override void _Notification(int what)
        {
            if (what == NotificationTranslationChanged)
                UpdateTooltip();

            base._Notification(what);
        }

        public override void _GuiInput(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseButton { Pressed: true } mouseEvent)
            {
                switch ((ButtonList)mouseEvent.ButtonIndex)
                {
                    case ButtonList.Left:
                        owner.Color = Color;
                        GetTree().SetInputAsHandled();
                        break;
                    case ButtonList.Right:
                        if (!owner.PresetsEnabled)
                            break;

                        owner.ErasePreset(Color);
                        GetTree().SetInputAsHandled();
                        break;
                }
            }

            base._GuiInput(inputEvent);
        }

        private void UpdateTooltip()
        {
            HintTooltip = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("COLOUR_PICKER_PRESET_TOOLTIP"), Color.ToHtml());
        }
    }

    private class PresetGroupStorage
    {
        public PresetGroupStorage(IEnumerable<Color> colours)
        {
            Colours = colours.ToList();
        }

        public AddPresetDelegate AddPresetDelegate { get; set; }

        public DeletePresetDelegate ErasePresetDelegate { get; set; }

        public List<Color> Colours { get; }

        public void AddPreset(Color colour)
        {
            if (Colours.Contains(colour))
                return;

            Colours.Add(colour);
            AddPresetDelegate(colour);
        }

        public void ErasePreset(Color colour)
        {
            Colours.Remove(colour);
            ErasePresetDelegate(colour);
        }
    }
}
