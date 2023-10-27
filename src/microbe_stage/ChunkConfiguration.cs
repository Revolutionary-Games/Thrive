using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   See FloatingChunk for what many of the fields here do
/// </summary>
public struct ChunkConfiguration : IEquatable<ChunkConfiguration>
{
    public string Name;

    /// <summary>
    ///   Possible models / scenes to use for this chunk
    /// </summary>
    public List<ChunkScene> Meshes;

    /// <summary>
    ///   This is the spawn density of the chunk
    /// </summary>
    public float Density;

    public bool Dissolves;
    public float Radius;
    public float ChunkScale;
    public float Mass;
    public float Size;

    /// <summary>
    ///   How much compound is vented per second
    /// </summary>
    public float VentAmount;

    /// <summary>
    ///   If > 0 the amount of damage to deal on touch
    /// </summary>
    public float Damages;

    public bool DeleteOnTouch;

    /// <summary>
    ///   The name of kind of damage type this chunk inflicts.
    /// </summary>
    public string DamageType;

    public Dictionary<Compound, ChunkCompound>? Compounds;

    /// <summary>
    ///   Whether this chunk type is an Easter egg.
    /// </summary>
    public bool EasterEgg;

    /// <summary>
    ///   The type of enzyme needed to break down this chunk.
    /// </summary>
    public string DissolverEnzyme;

    // TODO: convert the JSON data to directly specify the physics density
    [JsonIgnore]
    public float PhysicsDensity => Mass * 1000;

    public static bool operator ==(ChunkConfiguration left, ChunkConfiguration right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ChunkConfiguration left, ChunkConfiguration right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is ChunkConfiguration other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public bool Equals(ChunkConfiguration other)
    {
        return Name == other.Name &&
            Density == other.Density &&
            Dissolves == other.Dissolves &&
            Radius == other.Radius &&
            ChunkScale == other.ChunkScale &&
            Mass == other.Mass &&
            Size == other.Size &&
            VentAmount == other.VentAmount &&
            Damages == other.Damages &&
            DeleteOnTouch == other.DeleteOnTouch &&
            Meshes.Equals(other.Meshes) &&
            EasterEgg == other.EasterEgg &&
            DamageType == other.DamageType &&
            DissolverEnzyme == other.DissolverEnzyme &&
            Equals(Compounds, other.Compounds);
    }

    public struct ChunkCompound : IEquatable<ChunkCompound>
    {
        public float Amount;

        public static bool operator ==(ChunkCompound left, ChunkCompound right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkCompound left, ChunkCompound right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is ChunkCompound other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)Amount;
        }

        public bool Equals(ChunkCompound other)
        {
            return Amount == other.Amount;
        }
    }

    /// <summary>
    ///   Don't modify instances of this class
    /// </summary>
    public class ChunkScene
    {
        public string ScenePath = null!;

        /// <summary>
        ///   Path to the convex collision shape of this chunk's graphical mesh (if any).
        /// </summary>
        public string? ConvexShapePath;

        /// <summary>
        ///   Path to the MeshInstance inside the ScenePath scene, null if it is the root
        /// </summary>
        public string? SceneModelPath;

        /// <summary>
        ///   Path to the AnimationPlayer inside the ScenePath scene, null if no animation
        /// </summary>
        public string? SceneAnimationPath;

        /// <summary>
        ///   Need to be set to true on particle type visuals as those need special handling
        /// </summary>
        public bool IsParticles;

        /// <summary>
        ///   If true animations won't be stopped on this scene when this is spawned as a chunk
        /// </summary>
        public bool PlayAnimation;
    }
}
