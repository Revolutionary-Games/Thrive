using Godot;
using Newtonsoft.Json;

/// <summary>
///   Common HUD things for every HUD in the game
/// </summary>
[GodotAbstract]
public partial class HUDBase : Control, IStageHUD
{
#pragma warning disable CA2213
    [Export]
    protected PauseMenu menu = null!;

    [Export]
    private HUDMessages hudMessages = null!;
#pragma warning restore CA2213

    protected HUDBase()
    {
    }

    [JsonIgnore]
    public HUDMessages HUDMessages => hudMessages;

    public virtual void OnEnterStageTransition(bool longerDuration, bool returningFromEditor)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    public Control? GetFocusOwner()
    {
        return GetViewport().GuiGetFocusOwner();
    }

    /// <summary>
    ///   Fade into the stage for that smooth satisfying transition
    /// </summary>
    protected void AddFadeIn(IStageBase stageBase, bool longerDuration)
    {
        stageBase.TransitionFinished = false;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, longerDuration ? 1.0f : 0.5f,
            stageBase.OnFinishTransitioning);
    }
}
