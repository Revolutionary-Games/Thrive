using Godot;

/// <summary>
///   A dedicated screen where the player setup the settings they want to play with when starting a new game from a
///   descended game
/// </summary>
public partial class DescendSetupScreen : Node
{
    [Export]
    public NodePath? NewGameSettingsPath;

#pragma warning disable CA2213
    private NewGameSettings newGameSettings = null!;
#pragma warning restore CA2213

    public GameProperties? CurrentGame { get; set; }

    public override void _Ready()
    {
        if (CurrentGame == null)
        {
            GD.Print("Creating a new game to allow directly starting this scene");
            CurrentGame = GameProperties.StartAscensionStageGame(new WorldGenerationSettings());
        }

        newGameSettings = GetNode<NewGameSettings>(NewGameSettingsPath);

        newGameSettings.OpenFromDescendScreen(CurrentGame);

        // Show a bit of a fade in for this screen
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.5f, null, true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NewGameSettingsPath?.Dispose();
        }

        base.Dispose(disposing);
    }
}
