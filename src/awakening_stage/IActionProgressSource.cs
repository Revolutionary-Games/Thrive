using Godot;

/// <summary>
///   Information source for an action progress bar
/// </summary>
public interface IActionProgressSource
{
    public bool ActionInProgress { get; }

    /// <summary>
    ///   Progress of the current action between 0-1, needs only be valid when <see cref="ActionInProgress"/> is true
    /// </summary>
    public float ActionProgress { get; }

    /// <summary>
    ///   If not null this is added to the world position of this entity when considering where the displayed progress
    ///   bar is at
    /// </summary>
    public Vector3? ExtraProgressBarWorldOffset { get; }

    public bool GetAndConsumeActionSuccess();
}
