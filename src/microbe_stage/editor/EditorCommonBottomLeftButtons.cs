using Godot;

public class EditorCommonBottomLeftButtons : MarginContainer
{
    [Signal]
    public delegate void OnOpenMenu();

    [Signal]
    public delegate void OnOpenHelp();

    private void OnMenuButtonPressed()
    {
        EmitSignal(nameof(OnOpenMenu));
    }

    private void OnHelpButtonPressed()
    {
        EmitSignal(nameof(OnOpenHelp));
    }
}
