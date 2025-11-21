using Godot;

public interface IReadOnlyMetaball
{
    public Vector3 Position { get; }

    /// <summary>
    ///   The diameter of the metaball
    /// </summary>
    public float Size { get; }

    public float Radius => Size * 0.5f;

    public IReadOnlyMetaball? Parent { get; }

    /// <summary>
    ///   Basic rendering of the metaballs for now just uses a colour
    /// </summary>
    public Color Colour { get; }
}

public interface IReadonlyMacroscopicMetaball : IReadOnlyMetaball
{
    public IReadOnlyCellDefinition CellType { get; }
}
