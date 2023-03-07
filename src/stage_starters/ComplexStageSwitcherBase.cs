using Godot;

/// <summary>
///   Helper base class for making more complex stage starters than <see cref="SimpleStageSwitcher"/> can handle
/// </summary>
public abstract class ComplexStageSwitcherBase : Node
{
    [Export]
    public bool FadeFromBlack = true;

    private bool switchStarted;

    protected abstract bool UsesScenePreModification { get; }

    /// <summary>
    ///   When default <see cref="LoadScene"/> is used, this property determines which scene it loads
    /// </summary>
    protected abstract MainGameState SimplyLoadableGameState { get; }

    public override void _Ready()
    {
        base._Ready();

        if (FadeFromBlack)
        {
            TransitionManager.Instance.FadeOutInstantly();
        }
    }

    public override void _Process(float delta)
    {
        if (switchStarted)
            return;

        switchStarted = true;

        if (UsesScenePreModification)
        {
            GD.Print("Creating and modifying scene");
            var scene = LoadScene();

            CustomizeLoadedScene(scene);

            GD.Print("Switching to target scene");
            SceneManager.Instance.SwitchToScene(scene);
        }
        else
        {
            GD.Print("Switching to target scene");
            var scene = SwitchToStageScene();

            GD.Print("Post modifying target scene");
            CustomizeLoadedScene(scene);
        }
    }

    protected virtual Node SwitchToStageScene()
    {
        var scene = LoadScene();
        SceneManager.Instance.SwitchToScene(scene);

        return scene;
    }

    protected virtual Node LoadScene()
    {
        return SceneManager.Instance.LoadScene(SimplyLoadableGameState).Instance();
    }

    /// <summary>
    ///   Used by the derived classes to actually do the complex scene modification needed for this.
    ///   When this is called depends on <see cref="UsesScenePreModification"/>
    /// </summary>
    /// <param name="scene">The loaded scene to customize</param>
    protected abstract void CustomizeLoadedScene(Node scene);
}
