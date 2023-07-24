using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Access to membrane properties that are needed for caching generated membranes
/// </summary>
public interface IComputedMembraneData : ICacheableData
{
    public IReadOnlyList<Vector2> OrganellePositions { get; }
    public MembraneType Type { get; }
}

public static class MembraneComputationHelpers
{
    public static long ComputeMembraneDataHash(this IComputedMembraneData data)
    {
        var positions = data.OrganellePositions;

        unchecked
        {
            var nameHash = data.Type.InternalName.GetHashCode();
            long hash = 1409 + nameHash + ((long)nameHash << 28);

            hash ^= (positions.Count + 1) * 7793;
            int hashMultiply = 1;

            foreach (var position in positions)
            {
                var posHash = position.GetHashCode();
                hash ^= (hashMultiply * posHash) ^ ((5081L * hashMultiply * hashMultiply + posHash) << 32);
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
/// <remarks>
///   <para>
///     TODO: check if this needs to dispose the GeneratedMesh. That'll be a bit difficult as existing membranes
///     can still be using this object even when this is removed from the cache
///   </para>
/// </remarks>
public class ComputedMembraneData : IComputedMembraneData
{
    public ComputedMembraneData(IReadOnlyList<Vector2> organellePositions, MembraneType type, List<Vector2> vertices2D,
        ArrayMesh mesh, int surfaceIndex)
    {
        OrganellePositions = organellePositions;
        Type = type;
        Vertices2D = vertices2D;
        GeneratedMesh = mesh;
        SurfaceIndex = surfaceIndex;
    }

    public IReadOnlyList<Vector2> OrganellePositions { get; }
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
