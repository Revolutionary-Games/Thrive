/// <summary>
///   Allows a scene root to reject a broken Godot instantiation before the node enters the active scene tree.
/// </summary>
public interface ISceneInstanceValidator
{
    /// <summary>
    ///   Returns null when the scene instance is usable, or a short reason when it needs to be retried.
    /// </summary>
    public string? ValidateSceneInstance();
}
