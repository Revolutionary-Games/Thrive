using System.Collections.Generic;
using Godot;

/// <summary>
///   Tweaked color picker defines some custom ColorPicker functions.
/// </summary>
public class TweakedColourPicker : ColorPicker
{
    private readonly List<TweakedColorPickerPreset> presets = new List<TweakedColorPickerPreset>();
    private PackedScene presetScene;

    private ToolButton pickerButton;
    private CheckButton hsvCheckButton;
    private CheckButton rawCheckButton;
    private LineEdit hexColorEdit;
    private HBoxContainer presetsContainer;
    private Button addPresetButton;

    private bool hsvButtonEnabled = true;
    private bool rawButtonEnabled = true;

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

            if (hsvCheckButton == null)
                return;

            if (hsvButtonEnabled == false)
                hsvCheckButton.Disabled = true;
            else if (RawMode == false)
                hsvCheckButton.Disabled = false;
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

            if (rawCheckButton == null)
                return;

            if (rawButtonEnabled == false)
                rawCheckButton.Disabled = true;
            else if (HsvMode == false)
                rawCheckButton.Disabled = false;
        }
    }

    public new bool HsvMode
    {
        get => base.HsvMode;
        set
        {
            if (value && RawMode)
                return;

            base.HsvMode = value;
            hsvCheckButton.Pressed = value;
            if (rawCheckButton == null)
                return;

            if (value)
                rawCheckButton.Disabled = true;
            else if (rawButtonEnabled)
                rawCheckButton.Disabled = false;
        }
    }

    public new bool RawMode
    {
        get => base.RawMode;
        set
        {
            if (value && HsvMode)
                return;

            base.RawMode = value;
            rawCheckButton.Pressed = value;

            if (hsvCheckButton == null)
                return;

            if (value)
                hsvCheckButton.Disabled = true;
            else if (hsvButtonEnabled)
                hsvCheckButton.Disabled = false;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        presetScene = GD.Load<PackedScene>("res://src/gui_common/TweakedColorPickerPreset.tscn");

        GetChild(4).GetChild<Control>(4).Hide();
        GetChild<Control>(5).Hide();
        GetChild<Control>(6).Hide();
        GetChild<Control>(7).Hide();
        pickerButton = GetChild(1).GetChild<ToolButton>(1);

        hsvCheckButton = GetNode<CheckButton>("ButtonsContainer/HSVCheckButton");
        rawCheckButton = GetNode<CheckButton>("ButtonsContainer/RawCheckButton");
        hexColorEdit = GetNode<LineEdit>("ButtonsContainer/HexColorEdit");
        hexColorEdit.Text = "ffffff";

        presetsContainer = GetNode<HBoxContainer>("PresetContainer");
        addPresetButton = GetNode<Button>("PresetContainer/AddPresetButton");

        // Update button state.
        HSVButtonEnabled = hsvButtonEnabled;
        RawButtonEnabled = rawButtonEnabled;
        HsvMode = base.HsvMode;
        RawMode = base.RawMode;

        // Load initial presets.
        foreach (var color in GetPresets())
        {
            AddPresetLocal(color);
        }

        Translate();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            Translate();

        base._Notification(what);
    }

    public new void AddPreset(Color color)
    {
        foreach (var knownPreset in presets)
        {
            if (knownPreset.Color == color)
                return;
        }

        AddPresetLocal(color);

        base.AddPreset(color);
    }

    public new void ErasePreset(Color color)
    {
        foreach (var preset in presets)
        {
            if (preset.Color == color)
            {
                OnPresetDeleted(preset);
                break;
            }
        }
    }

    private void Translate()
    {
        pickerButton.HintTooltip = TranslationServer.Translate("COLOR_PICKER_PICK_COLOR");
        addPresetButton.HintTooltip = TranslationServer.Translate("COLOR_PICKER_ADD_PRESET");
        TweakedColorPickerPreset.HintTooltipBase = TranslationServer.Translate("COLOR") + ": #{0}\n"
            + TranslationServer.Translate("LEFT_MOUSE") + ": "
            + TranslationServer.Translate("COLOR_PICKER_SELECT_PRESET") + "\n"
            + TranslationServer.Translate("RIGHT_MOUSE") + ": "
            + TranslationServer.Translate("COLOR_PICKER_DELETE_PRESET");
    }

    private void AddPresetLocal(Color color)
    {
        var preset = presetScene.Instance<TweakedColorPickerPreset>();
        preset.Color = color;
        preset.Connect(nameof(TweakedColorPickerPreset.OnPresetSelected), this, nameof(OnPresetSelected));
        preset.Connect(nameof(TweakedColorPickerPreset.OnPresetDeleted), this, nameof(OnPresetDeleted));
        presets.Add(preset);
        presetsContainer.AddChild(preset);
    }

    private void OnAddPresetButtonPressed()
    {
        AddPreset(Color);
    }

    private void OnPresetSelected(Color color)
    {
        Color = color;
    }

    private void OnPresetDeleted(TweakedColorPickerPreset preset)
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

    private void OnColorChanged(Color color)
    {
        hexColorEdit.Text = color.a8 == 255 ? color.ToHtml(false) : color.ToHtml(true);
    }

    private void OnHexColorChanged(string color)
    {
        var caretPosition = hexColorEdit.CaretPosition;

        for (int i = 0; i < color.Length; i++)
        {
            if (!(color[i] >= '0' && color[i] <= '9') && !(color[i] >= 'A' && color[i] <= 'F') &&
                !(color[i] >= 'a' && color[i] <= 'f'))
            {
                color = color.Remove(i, 1);
                i--;
            }
        }

        if (hexColorEdit.Text != color)
        {
            hexColorEdit.Text = color;
            hexColorEdit.CaretPosition = caretPosition - 1;
        }

        if (color.Length == 6 || color.Length == 8)
            Color = new Color(color);
    }
}
