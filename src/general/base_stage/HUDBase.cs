using System;
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

    private DateTime loadingScreenStartTime;

    protected HUDBase()
    {
    }

    [JsonIgnore]
    public HUDMessages HUDMessages => hudMessages;

    public virtual void OnEnterStageLoadingScreen(bool longerDuration, bool returningFromEditor)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    public virtual void OnStageLoaded(IStageBase stageBase)
    {
        FadeInFromLoading(stageBase);
    }

    public Control? GetFocusOwner()
    {
        return GetViewport().GuiGetFocusOwner();
    }

    protected void ShowLoadingScreen(IStageBase stageBase)
    {
        stageBase.TransitionFinished = false;

        loadingScreenStartTime = DateTime.UtcNow;

        LoadingScreen.Instance.Show(Localization.Translate("LOADING_STAGE"), stageBase.GameState);
    }

    /// <summary>
    ///   Fade into the stage for that smooth satisfying transition
    /// </summary>
    protected void FadeInFromLoading(IStageBase stageBase)
    {
        stageBase.TransitionFinished = false;

        // Show a slightly longer animation if the loading screen has been shown for a short time to make it smoother
        bool longerDuration = (DateTime.UtcNow - loadingScreenStartTime).TotalSeconds < 0.8f;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, longerDuration ? 0.8f : 0.5f,
            () =>
            {
                stageBase.OnBlankScreenBeforeFadeIn();
                LoadingScreen.Instance.Hide();
            }, false);

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.5f,
            stageBase.OnFinishTransitioning, false, false);
    }
}
