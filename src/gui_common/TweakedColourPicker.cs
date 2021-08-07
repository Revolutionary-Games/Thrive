using Godot;

/// <summary>
///   Tweaked color picker defines some custom ColorPicker functions.
/// </summary>
public class TweakedColourPicker : ColorPicker
{
    private ToolButton pickerButton;
    private CheckButton hsvCheckButton;
    private CheckButton rawCheckButton;
    private Button addPresetButton;
    private LineEdit hexColorEdit;

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

        GetChild(4).GetChild<Control>(4).Hide();
        GetChild<Control>(5).Hide();
        GetChild<Control>(6).Hide();
        GetChild<Control>(7).Hide();
        pickerButton = GetChild(1).GetChild<ToolButton>(1);
        pickerButton.Connect("mouse_entered", this, nameof(OnMouseEnteredPickerButton));

        hsvCheckButton = GetNode<CheckButton>("ButtonsContainer/HSVCheckButton");
        rawCheckButton = GetNode<CheckButton>("ButtonsContainer/RawCheckButton");
        hexColorEdit = GetNode<LineEdit>("ButtonsContainer/HexColorEdit");
        hexColorEdit.Text = "ffffff";

        addPresetButton = GetNode<Button>("PresetContainer/AddPresetButton");

        // Update button state.
        HSVButtonEnabled = hsvButtonEnabled;
        RawButtonEnabled = rawButtonEnabled;
        HsvMode = base.HsvMode;
        RawMode = base.RawMode;
    }

    private void OnMouseEnteredPickerButton()
    {
        pickerButton.HintTooltip = TranslationServer.Translate("COLOR_PICKER_PICK_COLOR");
    }

    private void OnMouseEnteredAddPresetButton()
    {
        addPresetButton.HintTooltip = TranslationServer.Translate("COLOR_PICKER_ADD_PRESET");
    }

    private void OnAddPresetButtonPressed()
    {
        EmitSignal("preset_added");
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
            hexColorEdit.Text = color;

        if (color.Length == 6 || color.Length == 8)
            Color = new Color(color);
    }
}
