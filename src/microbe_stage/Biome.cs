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

    [JsonIgnore]
    public Texture LoadedIcon;

    public float AverageTemperature;

    public Dictionary<string, EnvironmentalCompoundProperties> Compounds;

    public Dictionary<string, ChunkConfiguration> Chunks;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (Name == string.Empty || Background == string.Empty)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Empty normal or damaged texture");
        }

        if (Compounds == null)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Compounds missing");
        }

        if (Chunks == null)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Chunks missing");
        }

        if (Icon == null)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "icon missing");
        }
    }

    /// <summary>
    ///   Loads the needed scenes for the chunks
    /// </summary>
    public void Resolve(SimulationParameters parameters)
    {
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

    public struct EnvironmentalCompoundProperties
    {
        public float Amount;
        public float Density;
        public float Dissolved;
    }

    /// <summary>
    ///   See FloatingChunk for what many of the fields here do
    /// </summary>
    public struct ChunkConfiguration
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

        public struct ChunkCompound
        {
            public float Amount;
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
}
