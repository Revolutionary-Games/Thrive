﻿using Godot;
using Newtonsoft.Json;
using SharedBase.Archive;
using ThriveScriptsShared;

/// <summary>
///   Base microbe biome with some parameters that are used for a Patch.
///   Modifiable versions of a Biome are stored in patches.
/// </summary>
public class Biome : RegistryType
{
    /// <summary>
    ///   Name of the biome, for showing to the player in the GUI
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    /// <summary>
    ///   References a Background by name
    /// </summary>
    public string Background = null!;

    /// <summary>
    ///   References a panorama resource path. The panorama images are backgrounds for 3D.
    /// </summary>
    public string Panorama = null!;

    /// <summary>
    ///   Icon of the biome to be used in the patch map
    /// </summary>
    public string Icon = null!;

    /// <summary>
    ///   The light to use for this biome
    /// </summary>
    public LightDetails Sunlight = new();

    public Color EnvironmentColour = new(0, 0, 0, 1);

    /// <summary>
    ///   How much the temperature in this biome varies on a microscopic scale when moving around
    /// </summary>
    public float TemperatureVarianceScale = 5;

    public float CompoundCloudBrightness = 1.0f;

    public WaterCurrentsDetails WaterCurrents = new();

    /// <summary>
    ///   Optional static terrain that is spawned when playing in this patch
    /// </summary>
    public TerrainConfiguration? Terrain;

    /// <summary>
    ///   Total gas volume of this biome when it is a single patch.
    /// </summary>
    public float GasVolume = 1;

    public MusicContext[]? ActiveMusicContexts = null;

    [JsonIgnore]
    public Texture2D? LoadedIcon;

    public BiomeConditions Conditions = null!;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonIgnore]
    public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.Biome;

    public static object ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        return SimulationParameters.Instance.GetBiome(ReadInternalName(reader, version));
    }

    public override void Check(string name)
    {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Background) || string.IsNullOrEmpty(Panorama))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Empty name background texture or panorama texture path");
        }

        if (Conditions == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Conditions missing");
        }

        Conditions.Check(name);

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

        if (CompoundCloudBrightness <= 0)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "cloud brightness needs to be over 0");
        }

        if (TemperatureVarianceScale < 0)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "temperature variance scale needs to be over 0");
        }

        if (EnvironmentColour.A < 1.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Environment colour alpha needs to be 1");
        }

        if (Terrain != null)
        {
            // Terrain will share our name
            Terrain.InternalName = name;
            Terrain.Check(name);
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void Resolve(SimulationParameters parameters)
    {
        LoadedIcon = GD.Load<Texture2D>(Icon);
    }

    public override void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return "Biome type: " + Name;
    }

    public class LightDetails
    {
        /// <summary>
        ///   Colour of the light
        /// </summary>
        public Color Colour = new(1, 1, 1, 1);

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
        public Vector3 Direction = new(0.25f, -0.3f, 0.75f);
    }

    public class WaterCurrentsDetails
    {
        /// <summary>
        ///   How much the currents push objects and clouds
        /// </summary>
        public float Speed = 1.0f;

        /// <summary>
        ///   How quickly the currents shift
        /// </summary>
        public float Chaoticness = 1.0f;

        /// <summary>
        ///   The reverse scale of the currents noise map. The higher this value, the more frequent the currents.
        /// </summary>
        public float InverseScale = 1.0f;

        /// <summary>
        ///   Whether the particle system should use trails.
        /// </summary>
        public bool UseTrails;

        public Color Colour = new(1.0f, 1.0f, 1.0f, 1.0f);

        public int ParticleCount = 300;
    }
}
