using System;
using System.Collections.Generic;

/// <summary>
///   Base microbe biome with some parameters that are used for a Patch.
///   Modifiable versions of a Biome are stored in patches.
/// </summary>
public class Biome : IRegistryType
{
    /// <summary>
    ///   Name of the biome, for showing to the player in the GUI
    /// </summary>
    public string Name;

    /// <summary>
    ///   References a Background by name
    /// </summary>
    public string Background;

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
    }

    public struct EnvironmentalCompoundProperties
    {
        public float Amount;
        public float Density;
        public float Dissolved;
    }

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
        public float VentAmount;
        public float Damages;
        public bool DeleteOnTouch;

        public Dictionary<string, ChunkCompound> Compounds;

        public struct ChunkCompound
        {
            public float Amount;
        }

        public struct ChunkScene
        {
            public string ScenePath;
        }
    }
}
