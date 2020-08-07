using System;
using System.Collections.Generic;
using Godot;
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

    public Dictionary<Compound, ChunkCompound> Compounds;

    public static bool operator ==(ChunkConfiguration left, ChunkConfiguration right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ChunkConfiguration left, ChunkConfiguration right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj)
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
            Compounds.Equals(other.Compounds);
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

        public override bool Equals(object obj)
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
    public class ChunkScene : ISaveLoadable
    {
        public string ScenePath;

        /// <summary>
        ///   Path to the MeshInstance inside the ScenePath scene, null if it is the root
        /// </summary>
        public string SceneModelPath;

        [JsonIgnore]
        public PackedScene LoadedScene;

        public void LoadScene()
        {
            LoadedScene = GD.Load<PackedScene>(ScenePath);
        }

        public void FinishLoading(ISaveContext context)
        {
            LoadScene();
        }
    }
}
