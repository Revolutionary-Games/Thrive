using Godot;
using Newtonsoft.Json;

[UseThriveConverter]
public abstract class Metaball
{
    public Vector3 Position { get; set; }
    public float Size { get; set; }

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
    ///   Calculates how many parent links need to be travelled to reach the root
    /// </summary>
    /// <returns>The number of hops to the root metaball</returns>
    public int CalculateTreeDepth()
    {
        if (Parent == null)
            return 0;

        return 1 + Parent.CalculateTreeDepth();
    }
}
