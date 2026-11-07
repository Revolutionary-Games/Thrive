using Godot;

/// <summary>
///   Selects a command-line workload before constructing the main menu.
/// </summary>
public partial class GameStartup : Node
{
    public override void _Ready()
    {
        if (SceneManager.QuitOrQuitting)
            return;

        Invoke.Instance.Perform(() =>
        {
            if (LaunchOptions.AutoEvo)
            {
                SceneManager.Instance.SwitchToScene(new AutoEvoCommandLine());
            }
            else
            {
                SceneManager.Instance.SwitchToScene("res://src/general/MainMenu.tscn");
            }
        });
    }
}
