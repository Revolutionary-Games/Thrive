using Godot;
using Newtonsoft.Json;

[UseThriveConverter]
public abstract class Metaball
{
    public Vector3 Position { get; set; }

    /// <summary>
    ///   The diameter of the metaball
    /// </summary>
    public float Size { get; set; } = 1.0f;

    /// <summary>
    ///   The radius of the metaball
    /// </summary>
    [JsonIgnore]
    public float Radius => Size * 0.5f;

    /// <summary>
    ///   For animation and convolution surfaces we need to know the structure of metaballs
    /// </summary>
    public Metaball? Parent { get; set; }

    /// <summary>
    ///   Basic rendering of the metaballs for now just uses a colour
    /// </summary>
    [JsonIgnore]
    public abstract Color Color { get; }

    /// <summary>
    ///   Checks if the data of this ball matches another (parent shouldn't be checked). Used for action replacement
    ///   detection.
    /// </summary>
    /// <param name="other">The other metaball to check against</param>
    /// <returns>True if these are fundamentally the same kind of placed ball</returns>
    public abstract bool MatchesDefinition(Metaball other);

    /// <summary>
    ///   Calculates how many parent links need to be travelled to reach the root
    /// </summary>
    /// <returns>The number of hops to the root metaball</returns>
    public int CalculateTreeDepth()
    {
        if (Parent == null)
            return 0;

        return 1 + Parent.CalculateTreeDepth();
    }

    /// <summary>
    ///   Checks the parent tree if the value is an ancestor of this
    /// </summary>
    /// <param name="potentialAncestor">The metaball to look for</param>
    /// <returns>True if the given metaball is this metaball's ancestor</returns>
    public bool HasAncestor(Metaball potentialAncestor)
    {
        if (Parent == null)
            return false;

        if (Parent == potentialAncestor)
            return true;

        return Parent.HasAncestor(potentialAncestor);
    }

    public override string ToString()
    {
        if (Parent == null)
            return $"Root Metaball at {Position}";

        return $"Metaball at {Position}";
    }
}
