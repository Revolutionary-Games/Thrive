using Godot;

/// <summary>
///   Tweaked color picker defines some custom ColorPicker functions.
/// </summary>
public class TweakedColourPicker : ColorPicker
{
    private readonly CheckButton hsvCheckButton;

    private readonly CheckButton rawCheckButton;

    private bool hsvModeDisabled;

    private bool rawModeDisabled;

    public TweakedColourPicker()
    {
        var baseNode = GetChild(4).GetChild(4);
        hsvCheckButton = baseNode.GetChild<CheckButton>(0);
        hsvCheckButton.Connect("toggled", this, nameof(OnHSVButtonToggled));
        rawCheckButton = baseNode.GetChild<CheckButton>(1);
        rawCheckButton.Connect("toggled", this, nameof(OnRawButtonToggled));
    }

    [Export]
    public bool HSVModeDisabled
    {
        get => hsvModeDisabled;
        set
        {
            hsvModeDisabled = value;
            UpdateControl();
        }
    }

    [Export]
    public bool RawModeDisabled
    {
        get => rawCheckButton.Disabled;
        set
        {
            rawModeDisabled = value;
            UpdateControl();
        }
    }

    public new bool HsvMode
    {
        get => base.HsvMode;
        set
        {
            if (hsvModeDisabled)
                return;

            base.HsvMode = value;
        }
    }

    public new bool RawMode
    {
        get => base.RawMode;
        set
        {
            if (rawModeDisabled)
                return;

            base.RawMode = value;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateControl();
    }

    private void OnHSVButtonToggled(bool isOn)
    {
        if (!isOn && rawModeDisabled)
            rawCheckButton.Disabled = true;
    }

    private void OnRawButtonToggled(bool isOn)
    {
        if (!isOn && hsvModeDisabled)
            hsvCheckButton.Disabled = true;
    }

    private void UpdateControl()
    {
        hsvCheckButton.Disabled = hsvModeDisabled;
        rawCheckButton.Disabled = rawModeDisabled;
    }
}
