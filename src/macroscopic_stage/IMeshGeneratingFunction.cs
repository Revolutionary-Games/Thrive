using Godot;

public interface IMeshGeneratingFunction
{
    public float SurfaceValue { get; set; }

    /// <summary>
    ///   Function value at a certain point. The higher the value, the closer the point is to the shape's center.
    /// </summary>
    public float GetValue(Vector3 pos);

    /// <summary>
    ///   Shape's color at a certain point.
    /// </summary>
    public Color GetColour(Vector3 pos);
}
