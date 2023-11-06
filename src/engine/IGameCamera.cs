using Godot;

/// <summary>
///   Base interface for game cameras that work to follow an entity
/// </summary>
public interface IGameCamera
{
    /// <summary>
    ///   Updates camera position to follow the object. Has to be called manually each frame (or update) by the system
    ///   owning the camera.
    /// </summary>
    public void UpdateCameraPosition(float delta, Vector3? followedObject);
}
