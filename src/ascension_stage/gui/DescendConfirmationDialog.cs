using Godot;

public class DescendConfirmationDialog : CustomConfirmationDialog
{
    private GameProperties? game;

    public void ShowForGame(GameProperties gameProperties)
    {
        game = gameProperties;
        PopupCenteredShrink();
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        PauseManager.Instance.AddPause(nameof(DescendConfirmationDialog));
    }

    protected override void OnClose()
    {
        PauseManager.Instance.Resume(nameof(DescendConfirmationDialog));
        base.OnClose();
    }

    private void OnConfirmed()
    {
        // Make sure this is closed to unpause the game
        Close();

        if (game == null)
        {
            GD.PrintErr("Current game not set");
            return;
        }

        GD.Print("Fading out the ascension stage");
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.5f, OnSwitchToSetupScreen, false);
    }

    private void OnSwitchToSetupScreen()
    {
        if (game == null)
        {
            GD.PrintErr("Current game has disappeared set");
            return;
        }

        var scene = GD.Load<PackedScene>("res://src/ascension_stage/gui/DescendSetupScreen.tscn")
            .Instance<DescendSetupScreen>();
        scene.CurrentGame = game;

        GD.Print("Switching to new game setup to finish descending");
        SceneManager.Instance.SwitchToScene(scene);
    }
}
