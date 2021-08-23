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
    /// <remarks>
    ///   KeyValuePair&lt;GetPath(), GetPresets()&gt;:
    ///   <ul>
    ///     <li> First parameter (string) is the string format of NodePath; </li>
    ///     <li> Second parameter (Color[]) is the preset colours to be stored. </li>
    ///   </ul>
    /// </remarks>
    /// </summary>
    private static readonly List<KeyValuePair<string, Color[]>> PresetsStorage
        = new List<KeyValuePair<string, Color[]>>();

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
    ///   Decide if user can toggle Raw CheckButton to switch Raw mode.
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
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // Hide replaced native controls. Can't delete them because it will crash Godot.
        GetChild(4).GetChild<Control>(4).Hide();
        GetChild<Control>(5).Hide();
        GetChild<Control>(6).Hide();
        GetChild<Control>(7).Hide();

        // Get controls
        var baseControl = GetChild(4);
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
        if (PresetsStorage.Exists(p => p.Key == GetPath()))
        {
            var presetsStored = PresetsStorage.First(p => p.Key == GetPath());
            foreach (var colour in presetsStored.Value)
                AddPreset(colour);
            PresetsStorage.Remove(presetsStored);
        }

        UpdateTooltips();
    }

    public override void _ExitTree()
    {
        // Store presets.
        PresetsStorage.Add(new KeyValuePair<string, Color[]>(GetPath(), GetPresets()));

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
        if (presets.Any(knownPreset => knownPreset.Color == colour))
            return;

        // Add preset locally
        var preset = new TweakedColourPickerPreset(this, colour);
        presets.Add(preset);
        presetsContainer.AddChild(preset);

        // Add preset to base class
        base.AddPreset(colour);
    }

    public new void ErasePreset(Color colour)
    {
        OnPresetDeleted(presets.First(p => p.Color == colour));
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
        AddPreset(Color);
    }

    private void OnPresetSelected(Color colour)
    {
        Color = colour;
        EmitSignal("colour_changed", Color);
    }

    private void OnPresetDeleted(TweakedColourPickerPreset preset)
    {
        presets.Remove(preset);
        presetsContainer.RemoveChild(preset);
        base.ErasePreset(preset.Color);
        preset.QueueFree();
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
        htmlColourEdit.Text = colour.a8 == 255 ? colour.ToHtml(false) : colour.ToHtml(true);
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
        public TweakedColourPickerPreset(TweakedColourPicker owner, Color colour)
        {
            Connect(nameof(OnPresetSelected), owner, nameof(OnPresetSelected));
            Connect(nameof(OnPresetDeleted), owner, nameof(OnPresetDeleted));
            Connect("gui_input", this, nameof(OnPresetGUIInput));

            Color = colour;
            MarginTop = MarginBottom = MarginLeft = MarginRight = 6.0f;
            RectMinSize = new Vector2(20, 20);
            SizeFlagsHorizontal = 8;
            SizeFlagsVertical = 4;
            UpdateTooltip();
        }

        [Signal]
        public delegate void OnPresetSelected(Color colour);

        [Signal]
        public delegate void OnPresetDeleted(TweakedColourPickerPreset preset);

        public override void _Notification(int what)
        {
            if (what == NotificationTranslationChanged)
                UpdateTooltip();

            base._Notification(what);
        }

        private void OnPresetGUIInput(InputEvent inputEvent)
        {
            if (!(inputEvent is InputEventMouseButton { Pressed: true } mouseEvent))
                return;

            switch ((ButtonList)mouseEvent.ButtonIndex)
            {
                case ButtonList.Left:
                    EmitSignal(nameof(OnPresetSelected), Color);
                    break;
                case ButtonList.Right:
                    EmitSignal(nameof(OnPresetDeleted), this);
                    break;
            }
        }

        private void UpdateTooltip()
        {
            HintTooltip = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("COLOUR_PICKER_PRESET_TOOLTIP"), Color.ToHtml());
        }
    }
}
