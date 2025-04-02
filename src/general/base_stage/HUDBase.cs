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

    private readonly TimeSpan minStageLoadingScreenDuration = TimeSpan.FromSeconds(1.0);

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

        // Fade in the loading screen. When loading a save, the load screen is already up and faded in.
        if (!stageBase.IsLoadedFromSave)
        {
            TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.35f, OnStartLoadingFinished, false,
                false);
        }

        LoadingScreen.Instance.Show(Localization.Translate("LOADING_STAGE"), stageBase.GameState);
    }

    /// <summary>
    ///   Fade into the stage for that smooth satisfying transition
    /// </summary>
    protected void FadeInFromLoading(IStageBase stageBase)
    {
        stageBase.TransitionFinished = false;

        // If the loading screen has not been shown for long enough, queue a retry
        if (DateTime.UtcNow - loadingScreenStartTime < minStageLoadingScreenDuration)
        {
            Invoke.Instance.QueueForObject(() => FadeInFromLoading(stageBase), this, true);
            return;
        }

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.35f,
            () =>
            {
                stageBase.OnBlankScreenBeforeFadeIn();
                LoadingScreen.Instance.Hide();
            }, false, false);

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.4f,
            stageBase.OnFinishTransitioning, false, false);
    }

    private void OnStartLoadingFinished()
    {
        loadingScreenStartTime = DateTime.UtcNow;
    }
}
