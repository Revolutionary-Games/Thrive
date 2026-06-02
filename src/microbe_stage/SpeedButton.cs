using Godot;

/// <summary>
///   Handles speed button label text and colour.
/// </summary>
public partial class SpeedButton : TextureButton
{
    private bool available = true;

#pragma warning disable CA2213
    [Export]
    private Label label = null!;
#pragma warning restore CA2213

    [Export]
    public bool SpeedButtonAvailable
    {
        get => available;
        set
        {
            available = value;
            Visible = available;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        Visible = available;
        OnChanged(Settings.Instance.AlternativeTimescale.Value);
        Settings.Instance.AlternativeTimescale.OnChanged += OnChanged;
        Toggled += OnToggled;
    }

    public void OnToggled(bool value)
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetParent().EmitSignal(HUDBottomBar.SignalName.OnSpeedModeToggled, value);
        label.Modulate = value ? Colors.Black : Colors.White;
    }

    private void OnChanged(float value)
    {
        label.LabelSettings.FontSize = value % 1 == 0 ? 21 : 14;
        label.Text = value + "x";
    }
}
