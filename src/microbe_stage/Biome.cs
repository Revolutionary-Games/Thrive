using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base microbe biome with some parameters that are used for a Patch.
///   Modifiable versions of a Biome are stored in patches.
/// </summary>
public class Biome : IRegistryType, ICloneable
{
    /// <summary>
    ///   Name of the biome, for showing to the player in the GUI
    /// </summary>
    public string Name;

    /// <summary>
    ///   References a Background by name
    /// </summary>
    public string Background;

    /// <summary>
    ///   Icon of the biome to be used in the patch map
    /// </summary>
    public string Icon;

    /// <summary>
    ///   The light to use for this biome
    /// </summary>
    public LightDetails Sunlight;

    [JsonIgnore]
    public Texture LoadedIcon;

    public float AverageTemperature;

    public Dictionary<string, EnvironmentalCompoundProperties> Compounds;

    public Dictionary<string, ChunkConfiguration> Chunks;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Background))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Empty normal or damaged texture");
        }

        if (Compounds == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compounds missing");
        }

        if (Chunks == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Chunks missing");
        }

        if (Icon == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "icon missing");
        }

        if (Sunlight == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "sunlight missing");
        }
    }

    /// <summary>
    ///   Loads the needed scenes for the chunks
    /// </summary>
    public void Resolve(SimulationParameters parameters)
    {
        _ = parameters;

        foreach (var entry in Chunks)
        {
            foreach (var meshEntry in entry.Value.Meshes)
            {
                meshEntry.LoadedScene = GD.Load<PackedScene>(meshEntry.ScenePath);
            }
        }

        LoadedIcon = GD.Load<Texture>(Icon);
    }

    public object Clone()
    {
        Biome result = new Biome
        {
            Name = Name,
            Background = Background,
            AverageTemperature = AverageTemperature,
            InternalName = InternalName,
            Compounds = new Dictionary<string, EnvironmentalCompoundProperties>(
                Compounds.Count),
            Chunks = new Dictionary<string, ChunkConfiguration>(Chunks.Count),
            Icon = Icon,
            LoadedIcon = LoadedIcon,

            // Clone this as well if needed (if the light properties can change)
            Sunlight = Sunlight,
        };

        foreach (var entry in Compounds)
        {
            result.Compounds.Add(entry.Key, entry.Value);
        }

        foreach (var entry in Chunks)
        {
            result.Chunks.Add(entry.Key, entry.Value);
        }

        return result;
    }

    public struct EnvironmentalCompoundProperties : IEquatable<EnvironmentalCompoundProperties>
    {
        public float Amount;
        public float Density;
        public float Dissolved;

        public static bool operator ==(EnvironmentalCompoundProperties left, EnvironmentalCompoundProperties right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EnvironmentalCompoundProperties left, EnvironmentalCompoundProperties right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is EnvironmentalCompoundProperties other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)(Amount + Density + Dissolved);
        }

        public bool Equals(EnvironmentalCompoundProperties other)
        {
            return Amount == other.Amount && Density == other.Density && Dissolved == other.Dissolved;
        }
    }

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

        public Dictionary<string, ChunkCompound> Compounds;

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
        public class ChunkScene
        {
            public string ScenePath;

            [JsonIgnore]
            public PackedScene LoadedScene;
        }
    }

    public class LightDetails
    {
        /// <summary>
        ///   Colour of the light
        /// </summary>
        public Color Colour = new Color(1, 1, 1, 1);

        /// <summary>
        ///   Strength of the light
        /// </summary>
        public float Energy = 1.0f;

        /// <summary>
        ///   How much specular there is
        /// </summary>
        public float Specular = 0.5f;

        /// <summary>
        ///   Shadow casting enabled / disabled
        /// </summary>
        public bool Shadows = true;

        /// <summary>
        ///   The direction the light is pointing at. This is done by placing the light and making it look at a relative
        ///   position with these coordinates.
        /// </summary>
        public Vector3 Direction = new Vector3(0.25f, -0.3f, 0.75f);
    }
}
