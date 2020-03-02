using System;
using Godot;

/// <summary>
///   Definition of a compound in the game. For all other simulation
///   parameters that refer to a compound, there must be an existing
///   entry of this type
/// </summary>
public class Compound : IRegistryType
{
    /// <summary>
    ///   Display name for the user to see
    /// </summary>
    public string Name;

    public float Volume;

    public bool IsCloud;

    [Obsolete("This is now inferred from the used processes")]
    public bool IsUseful;

    public bool IsEnvironmental;

    public Color Colour;

    public void Check(string name)
    {
        if (Name == string.Empty)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Compound has no name");
        }

        // Guards against uninitialized alpha
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
        if (Colour.a == 0.0f)
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
            Colour.a = 1;

        if (Math.Abs(Colour.a - 1.0f) > MathUtils.EPSILON)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Compound colour cannot have alpha other than 1");
        }

        if (Math.Abs(Colour.r) < MathUtils.EPSILON &&
        Math.Abs(Colour.g) < MathUtils.EPSILON && Math.Abs(Colour.b) < MathUtils.EPSILON)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Compound colour can't be black");
        }

        if (Volume <= 0)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Volume should be > 0");
        }
    }
}
