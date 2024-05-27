using Godot;

/// <summary>
///   Utility extensions for Godot vector types
/// </summary>
public static class VectorUtils
{
    public static Vector2 AsFloats(this in Vector2I vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    public static Vector2I RoundedInt(this in Vector2 vector2)
    {
        return new Vector2I(Mathf.RoundToInt(vector2.X), Mathf.RoundToInt(vector2.Y));
    }
}
