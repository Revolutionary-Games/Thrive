using Godot;

/// <summary>
///   Handles speed button label text and colour.
/// </summary>
public partial class SpeedButton : TextureButton
{
#pragma warning disable CA2213
    [Export]
    private LabelSettings normalSize = null!;

    [Export]
    private LabelSettings withDecimalSize = null!;

    [Export]
    private Label label = null!;

#pragma warning restore CA2213

    public override void _EnterTree()
    {
        base._EnterTree();
        OnChanged(Settings.Instance.AlternativeTimescale.Value);
        Settings.Instance.AlternativeTimescale.OnChanged += OnChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Settings.Instance.AlternativeTimescale.OnChanged -= OnChanged;
    }

    public void ApplyState(bool value)
    {
        label.Modulate = value ? Colors.Black : Colors.White;
    }

    private void OnChanged(float value)
    {
        label.LabelSettings = value % 1 == 0 ? normalSize : withDecimalSize;
        label.Text = value + "x";
    }
}
