using Godot;

/// <summary>
///   Tweaked color picker defines some custom ColorPicker functions.
/// </summary>
public class TweakedColourPicker : ColorPicker
{
    private readonly CheckButton hsvCheckButton;
    private readonly CheckButton rawCheckButton;
    private readonly ToolButton pickerButton;
    private readonly Button addPresetButton;

    private bool hsvButtonDisabled;
    private bool rawButtonDisabled;

    public TweakedColourPicker()
    {
        var baseNode = GetChild(4).GetChild(4);
        hsvCheckButton = baseNode.GetChild<CheckButton>(0);
        hsvCheckButton.Connect("toggled", this, nameof(OnHSVButtonToggled));
        rawCheckButton = baseNode.GetChild<CheckButton>(1);
        rawCheckButton.Connect("toggled", this, nameof(OnRawButtonToggled));

        pickerButton = GetChild(1).GetChild<ToolButton>(1);
        pickerButton.Connect("mouse_entered", this, nameof(OnMouseEnteredPickerButton));

        addPresetButton = GetChild(7).GetChild<Button>(0);
        addPresetButton.Connect("mouse_entered", this, nameof(OnMouseEnteredAddPresetButton));
    }

    /// <summary>
    ///   Decide if user can toggle HSV CheckButton to switch HSV mode.
    /// </summary>
    [Export]
    public bool HSVButtonDisabled
    {
        get => hsvButtonDisabled;
        set
        {
            hsvButtonDisabled = value;
            UpdateButtonsState();
        }
    }

    /// <summary>
    ///   Decide if user can toggle Raw CheckButton to switch Raw mode.
    /// </summary>
    [Export]
    public bool RawButtonDisabled
    {
        get => rawButtonDisabled;
        set
        {
            rawButtonDisabled = value;
            UpdateButtonsState();
        }
    }

    // To ensure (after Godot made changes)
    public new bool HsvMode
    {
        get => base.HsvMode;
        set
        {
            base.HsvMode = value;
            UpdateButtonsState();
        }
    }

    public new bool RawMode
    {
        get => base.RawMode;
        set
        {
            base.RawMode = value;
            UpdateButtonsState();
        }
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateButtonsState();
    }

    public void OnMouseEnteredPickerButton()
    {
        pickerButton.HintTooltip = TranslationServer.Translate("COLOR_PICKER_PICK_COLOR");
    }

    public void OnMouseEnteredAddPresetButton()
    {
        addPresetButton.HintTooltip = TranslationServer.Translate("COLOR_PICKER_ADD_PRESET");
    }

    private void OnHSVButtonToggled(bool isOn)
    {
        if (!isOn && rawButtonDisabled)
            rawCheckButton.Disabled = true;
    }

    private void OnRawButtonToggled(bool isOn)
    {
        if (!isOn && hsvButtonDisabled)
            hsvCheckButton.Disabled = true;
    }

    private void UpdateButtonsState()
    {
        hsvCheckButton.Disabled = hsvButtonDisabled;
        rawCheckButton.Disabled = rawButtonDisabled;
    }
}
