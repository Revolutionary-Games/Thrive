using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Tweaked color picker defines some custom ColorPicker functions.
/// </summary>
public class TweakedColourPicker : ColorPicker
{
    /// <summary>
    ///   This is where presets are stored after a color picker exited scene tree.
    /// <remarks>
    ///   KeyValuePair&lt;GetPath(), GetPresets()&gt;:
    ///   <ul>
    ///     <li> First parameter (string) is the string format of NodePath; </li>
    ///     <li> Second parameter (Color[]) is the preset colors to be stored. </li>
    ///   </ul>
    /// </remarks>
    /// </summary>
    private static readonly List<KeyValuePair<string, Color[]>> PresetsStorage
        = new List<KeyValuePair<string, Color[]>>();

    private readonly List<TweakedColorPickerPreset> presets = new List<TweakedColorPickerPreset>();

    private ToolButton pickerButton;
    private CheckButton hsvCheckButton;
    private CheckButton rawCheckButton;
    private LineEdit hexColorEdit;
    private HSeparator separator;
    private GridContainer presetsContainer;
    private Button addPresetButton;

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

        pickerButton = GetChild(1).GetChild<ToolButton>(1);
        hsvCheckButton = GetNode<CheckButton>("ButtonsContainer/HSVCheckButton");
        rawCheckButton = GetNode<CheckButton>("ButtonsContainer/RawCheckButton");
        hexColorEdit = GetNode<LineEdit>("ButtonsContainer/HexColorEdit");
        hexColorEdit.Text = Color.ToHtml();
        separator = GetNode<HSeparator>("Separator");
        presetsContainer = GetNode<GridContainer>("PresetContainer");
        addPresetButton = GetNode<Button>("PresetContainer/AddPresetButton");

        // Update control state.
        UpdateButtonsState();
        PresetsEnabled = presetsEnabled;
        PresetsVisible = presetsVisible;

        // Load presets.
        if (PresetsStorage.Exists(p => p.Key == GetPath()))
        {
            var presetsStored = PresetsStorage.First(p => p.Key == GetPath());
            foreach (var color in presetsStored.Value)
                AddPreset(color);
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

    public new void AddPreset(Color color)
    {
        if (presets.Any(knownPreset => knownPreset.Color == color))
            return;

        // Add preset locally
        var preset = new TweakedColorPickerPreset(this, color);
        presets.Add(preset);
        presetsContainer.AddChild(preset);

        // Add preset to base class
        base.AddPreset(color);
    }

    public new void ErasePreset(Color color)
    {
        OnPresetDeleted(presets.First(p => p.Color == color));
    }

    private void UpdateTooltips()
    {
        pickerButton.HintTooltip = TranslationServer.Translate("COLOR_PICKER_PICK_COLOR");
        addPresetButton.HintTooltip = TranslationServer.Translate("COLOR_PICKER_ADD_PRESET");
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

    private void OnPresetSelected(Color color)
    {
        Color = color;
        EmitSignal("color_changed", Color);
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

    /// <summary>
    ///   Called when HexColorEdit's text has changed (because of user input / color select). In this function,
    ///   the input has been confirmed, so if we find something wrong (such as illegal input), we should delete it
    ///   and set the text to a legal modified one. And because caret position is set to 0 after a '.Text = ""', we
    ///   need to manually adjust the caret position to avoid strange behavior.
    /// </summary>
    /// <param name="color">HexColorEdit's text content</param>
    private void OnHexColorChanged(string color)
    {
        // Get initial caret position
        var caretPosition = hexColorEdit.CaretPosition;

        // Enum color text
        for (int i = 0; i < color.Length; i++)
        {
            // Legal characters are "0123456789abcdefABCDEF"
            if (!(color[i] >= '0' && color[i] <= '9') && !(color[i] >= 'A' && color[i] <= 'F') &&
                !(color[i] >= 'a' && color[i] <= 'f'))
            {
                // Remove this character, and when enum to the next, index should not change.
                color = color.Remove(i--, 1);

                // Cursor position should always be after the thing he keyed in
                caretPosition--;
            }
        }

        // If some changes has been made
        if (hexColorEdit.Text != color)
        {
            hexColorEdit.Text = color;
            hexColorEdit.CaretPosition = caretPosition;
        }

        // If color is valid
        if (color.Length == 6 || color.Length == 8)
            Color = new Color(color);
    }
}

public class TweakedColorPickerPreset : ColorRect
{
    public TweakedColorPickerPreset(TweakedColourPicker owner, Color color)
    {
        Connect(nameof(OnPresetSelected), owner, nameof(OnPresetSelected));
        Connect(nameof(OnPresetDeleted), owner, nameof(OnPresetDeleted));
        Connect("gui_input", this, nameof(OnPresetGUIInput));

        Color = color;
        MarginTop = MarginBottom = MarginLeft = MarginRight = 6.0f;
        RectMinSize = new Vector2(20, 20);
        SizeFlagsHorizontal = 8;
        SizeFlagsVertical = 4;
        UpdateTooltip();
    }

    [Signal]
    public delegate void OnPresetSelected(Color color);

    [Signal]
    public delegate void OnPresetDeleted(TweakedColorPickerPreset preset);

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            UpdateTooltip();

        base._Notification(what);
    }

    private void OnPresetGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true } mouseEvent)
        {
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
    }

    private void UpdateTooltip()
    {
        HintTooltip = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("COLOR_PICKER_PRESET_TOOLTIP"), Color.ToHtml());
    }
}
