﻿using Godot;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   Defines properties of a membrane type
/// </summary>
public class MembraneType : IRegistryType
{
    /// <summary>
    ///   User readable name
    /// </summary>
    [TranslateFrom(nameof(UntranslatedName))]
    public string Name = null!;

    [JsonRequired]
    public string IconPath = null!;

    public string AlbedoTexture = null!;
    public string NormalTexture = null!;
    public string WiggleNormalTexture = null!;
    public string DamagedTexture = null!;
    public float MovementFactor = 1.0f;
    public float OsmoregulationFactor = 1.0f;
    public float ResourceAbsorptionFactor = 1.0f;
    public int Hitpoints = 100;
    public float PhysicalResistance = 1.0f;
    public float ToxinResistance = 1.0f;
    public int EditorCost = 50;
    public bool CellWall;
    public float BaseWigglyness = 1.0f;
    public float MovementWigglyness = 1.0f;

    /// <summary>
    ///   Type of enzyme capable of dissolving this membrane type. Default is lipase.
    /// </summary>
    public string DissolverEnzyme = "lipase";

    public int EditorButtonOrder;

    [JsonIgnore]
    public Texture2D LoadedAlbedoTexture = null!;

    [JsonIgnore]
    public Texture2D LoadedNormalTexture = null!;

    [JsonIgnore]
    public Texture2D LoadedWiggleNormalTexture = null!;

    [JsonIgnore]
    public Texture2D LoadedDamagedTexture = null!;

    [JsonIgnore]
    public Texture2D? LoadedIcon;

    public string InternalName { get; set; } = null!;

    [JsonIgnore]
    public string UntranslatedName { get; private set; } = null!;

    [JsonIgnore]
    public bool CanEngulf => !CellWall;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(IconPath))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing IconPath");
        }

        if (string.IsNullOrEmpty(AlbedoTexture) || string.IsNullOrEmpty(NormalTexture)
            || string.IsNullOrEmpty(DamagedTexture))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Empty albedo, normal, or damaged texture");
        }

#if DEBUG
        string[] membranes = { AlbedoTexture, NormalTexture, DamagedTexture };

        foreach (var resource in membranes)
        {
            if (!ResourceLoader.Exists(resource))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Membrane uses non-existent image: " + resource);
            }
        }
#endif

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    /// <summary>
    ///   Resolves references to external resources so that during
    ///   runtime they don't need to be looked up
    /// </summary>
    public void Resolve()
    {
        LoadedAlbedoTexture = GD.Load<Texture2D>(AlbedoTexture);
        LoadedNormalTexture = GD.Load<Texture2D>(NormalTexture);
        LoadedWiggleNormalTexture = GD.Load<Texture2D>(WiggleNormalTexture);
        LoadedDamagedTexture = GD.Load<Texture2D>(DamagedTexture);

        LoadedIcon = GD.Load<Texture2D>(IconPath);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
