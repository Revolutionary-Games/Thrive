using Godot;

/// <summary>
///   Controls the process panel contents
/// </summary>
public class ProcessPanel : WindowDialog
{
    public override void _Ready()
    {
    }

    public override void _Process(float delta)
    {
        if (!IsVisibleInTree())
            return;
    }

    private void ClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Visible = false;
    }
}
