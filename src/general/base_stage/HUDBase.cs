using Godot;
using Newtonsoft.Json;

/// <summary>
///   Common HUD things for every HUD in the game
/// </summary>
public abstract class HUDBase : Control, IStageHUD
{
    [Export]
    public NodePath? MenuPath;

    [Export]
    public NodePath HUDMessagesPath = null!;

#pragma warning disable CA2213
    protected PauseMenu menu = null!;
    private HUDMessages hudMessages = null!;
#pragma warning restore CA2213

    [JsonIgnore]
    public HUDMessages HUDMessages => hudMessages;

    public override void _Ready()
    {
        base._Ready();

        menu = GetNode<PauseMenu>(MenuPath);
        hudMessages = GetNode<HUDMessages>(HUDMessagesPath);
    }

    public abstract void OnEnterStageTransition(bool longerDuration, bool returningFromEditor);

    protected void AddFadeIn(IStageBase stageBase, bool longerDuration)
    {
        // Fade out for that smooth satisfying transition
        stageBase.TransitionFinished = false;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, longerDuration ? 1.0f : 0.5f,
            stageBase.OnFinishTransitioning);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MenuPath != null)
            {
                MenuPath.Dispose();
                HUDMessagesPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
