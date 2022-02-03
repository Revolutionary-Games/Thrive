﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Background in the microbe stage, needs to have 4 layers (textures)
/// </summary>
[SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "This is a read-only type")]
public class Background : IRegistryType
{
    [JsonRequired]
    public List<string> Textures = null!;

    [JsonRequired]
    public string ParticleEffect = null!;

    [JsonIgnore]
    public PackedScene ParticleEffectScene = null!;

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (Textures == null || Textures.Count != 4)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Background needs 4 layers");
        }

        if (string.IsNullOrEmpty(ParticleEffect))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "ParticleEffect is missing");
        }
    }

    /// <summary>
    ///   Checks that resource paths are valid. This doesn't preload the images as they are big and there are a lot of
    ///   them.
    /// </summary>
    public void Resolve(SimulationParameters parameters)
    {
        _ = parameters;

        var directory = new Directory();

        foreach (var resource in Textures)
        {
            // When exported only the .import files exist, so this check is done accordingly
            if (!directory.FileExists(resource + ".import"))
            {
                throw new InvalidRegistryDataException(InternalName, GetType().Name,
                    "Background contains non-existent image: " + resource);
            }
        }

        ParticleEffectScene = GD.Load<PackedScene>(ParticleEffect);
    }

    public void ApplyTranslations()
    {
    }
}
