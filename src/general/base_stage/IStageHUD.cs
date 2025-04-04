using Godot;

/// <summary>
///   Basic universal properties of all HUD types for the stages
/// </summary>
public interface IStageHUD
{
    public HUDMessages HUDMessages { get; }

    public void OnEnterStageLoadingScreen(bool longerDuration, bool returningFromEditor);

    /// <summary>
    ///   Called after the stage is done (by the stage) loading resources and the loading screen can end
    /// </summary>
    public void OnStageLoaded(IStageBase stageBase);

    public Control? GetFocusOwner();
}
