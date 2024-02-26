using Godot;

public partial class SimpleStageStarter : Node
{
    [Export]
    public MainGameState StageToSwitchTo = MainGameState.Invalid;

    [Export]
    public bool FadeFromBlack = true;

    private bool switchStarted;

    public override void _Ready()
    {
        if (StageToSwitchTo == MainGameState.Invalid)
            GD.PrintErr("This scene is not setup correctly, stage to switch to is not set");

        // Some scenes look better when switched to by blanking to a black screen first
        if (FadeFromBlack)
        {
            TransitionManager.Instance.FadeOutInstantly();
        }
    }

    public override void _Process(double delta)
    {
        if (switchStarted)
            return;

        switchStarted = true;

        // We can't switch scenes in _Ready as the game is initializing still

        GD.Print("Switching to scene: ", StageToSwitchTo);

        SceneManager.Instance.SwitchToScene(StageToSwitchTo);
    }
}
