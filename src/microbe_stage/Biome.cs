﻿using Godot;
using Newtonsoft.Json;

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

    public BiomeConditions Conditions;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Background))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Empty normal or damaged texture");
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
    }

    /// <summary>
    ///   Loads the needed scenes for the chunks
    /// </summary>
    public void Resolve(SimulationParameters parameters)
    {
        Conditions.Resolve(parameters);

        LoadedIcon = GD.Load<Texture>(Icon);
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
