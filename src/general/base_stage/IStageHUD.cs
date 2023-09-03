using Godot;

/// <summary>
///   Basic universal properties of all HUD types for the stages
/// </summary>
public interface IStageHUD
{
    public HUDMessages HUDMessages { get; }

    public void OnEnterStageTransition(bool longerDuration, bool returningFromEditor);

    public Control? GetFocusOwner();
}
