using Godot;

/// <summary>
///   Calculations and other operations for the strategic camera
/// </summary>
public static class StrategicCameraHelpers
{
    /// <summary>
    ///   Given a world "focus" point, this calculates where the strategic camera should be at
    /// </summary>
    /// <param name="worldPositionToLook">The world point</param>
    /// <param name="zoomLevel">How close the camera should be, 1 is default zoom level</param>
    /// <returns>Transform for camera</returns>
    public static Transform CalculateCameraPosition(Vector3 worldPositionToLook, float zoomLevel)
    {
        // TODO: actual camera positioning logic
        var cameraPos = worldPositionToLook + new Vector3(0, zoomLevel * 50 + 5, -1 * (zoomLevel * 20 + 10));

        return new Transform(Basis.Identity, cameraPos).LookingAt(worldPositionToLook, Vector3.Up);
    }
}
