using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Access to membrane properties that are needed for caching generated membranes
/// </summary>
public interface IComputedMembraneData : ICacheableData
{
    public List<Vector2> OrganellePositions { get; }
    public MembraneType Type { get; }
}

public static class MembraneComputationHelpers
{
    public static long ComputeMembraneDataHash(this IComputedMembraneData data)
    {
        var positions = data.OrganellePositions;

        unchecked
        {
            long hash = 31 * data.Type.InternalName.GetHashCode();

            hash += (positions.Count + 1) * 7793;
            int hashMultiply = 1;

            foreach (var position in positions)
            {
                hash ^= hashMultiply * (position.GetHashCode() + 5081);
                ++hashMultiply;
            }

            return hash;
        }
    }

    public static bool MembraneDataFieldsEqual(this IComputedMembraneData data, IComputedMembraneData other)
    {
        return data.Type.Equals(other.Type) && data.OrganellePositions.SequenceEqual(other.OrganellePositions);
    }
}

/// <summary>
///   Final, computed data for a membrane. This is a separate class to support caching this
/// </summary>
public class ComputedMembraneData : IComputedMembraneData
{
    public ComputedMembraneData(List<Vector2> organellePositions, MembraneType type, List<Vector2> vertices2D,
        ArrayMesh mesh, int surfaceIndex)
    {
        OrganellePositions = organellePositions;
        Type = type;
        Vertices2D = vertices2D;
        GeneratedMesh = mesh;
        SurfaceIndex = surfaceIndex;
    }

    public List<Vector2> OrganellePositions { get; }
    public MembraneType Type { get; }
    public List<Vector2> Vertices2D { get; }

    public ArrayMesh GeneratedMesh { get; }
    public int SurfaceIndex { get; }

    public bool MatchesCacheParameters(ICacheableData cacheData)
    {
        if (cacheData is IComputedMembraneData data)
            return this.MembraneDataFieldsEqual(data);

        return false;
    }

    public long ComputeCacheHash()
    {
        return this.ComputeMembraneDataHash();
    }
}
