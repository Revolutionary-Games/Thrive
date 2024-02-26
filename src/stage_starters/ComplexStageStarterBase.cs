using Godot;

/// <summary>
///   Helper base class for making more complex stage starters than <see cref="SimpleStageStarter"/> can handle
/// </summary>
public abstract partial class ComplexStageStarterBase : Node
{
    [Export]
    public bool FadeFromBlack = true;

    private bool switchStarted;

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

    public override void _Process(double delta)
    {
        if (switchStarted)
            return;

        switchStarted = true;

        GD.Print("Creating and modifying scene");
        var scene = LoadScene();

        CustomizeLoadedScene(scene);

        GD.Print("Switching to target scene");
        SceneManager.Instance.SwitchToScene(scene);

        CustomizeAttachedScene(scene);
    }

    protected virtual Node SwitchToStageScene()
    {
        var scene = LoadScene();
        SceneManager.Instance.SwitchToScene(scene);

        return scene;
    }

    protected virtual Node LoadScene()
    {
        return SceneManager.Instance.LoadScene(SimplyLoadableGameState).Instantiate();
    }

    /// <summary>
    ///   Used by the derived classes to actually do the complex scene modification needed for this.
    ///   This is called after loading the scene but before attaching it.
    /// </summary>
    /// <param name="scene">The loaded scene to customize</param>
    protected abstract void CustomizeLoadedScene(Node scene);

    /// <summary>
    ///   Extra scene modification step that is called after the scene has been attached
    /// </summary>
    /// <param name="scene">The attached scene</param>
    protected virtual void CustomizeAttachedScene(Node scene)
    {
    }
}
