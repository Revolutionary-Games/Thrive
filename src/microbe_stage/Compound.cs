using System;
using System.ComponentModel;
using Godot;

/// <summary>
///   Definition of a compound in the game. For all other simulation
///   parameters that refer to a compound, there must be an existing
///   entry of this type
/// </summary>
[TypeConverter(typeof(CompoundStringConverter))]
public class Compound : IRegistryType
{
    /// <summary>
    ///   Display name for the user to see
    /// </summary>
    [TranslateFrom("untranslatedName")]
    public string Name = null!;

    public float Volume;

    public bool IsCloud;

    public string IconPath = null!;

    public string TexturePath = null!;

    /// <summary>
    ///   When this is true the compound is always considered to be
    ///   useful and is not dumped.
    /// </summary>
    public bool IsAlwaysUseful;

    public bool IsEnvironmental;

    /// <summary>
    ///   Whether this compound can be distributed in a colony
    /// </summary>
    public bool CanBeDistributed;

    public Color Colour;

    /// <summary>
    ///   Loaded icon for display in GUIs
    /// </summary>
    public Texture? LoadedIcon;

    /// <summary>
    ///   Loaded texture for display on compound clouds (to set compounds more clearly apart)
    /// </summary>
    public Texture? LoadedTexture;

#pragma warning disable 169 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compound has no name");
        }

        if (string.IsNullOrEmpty(IconPath))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compound must be provided an icon");
        }

        if (string.IsNullOrEmpty(TexturePath) && IsCloud)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compound must be provided with a texture");
        }

        // Guards against uninitialized alpha
#pragma warning disable RECS0018
        if (Colour.a == 0.0f)
#pragma warning restore RECS0018
            Colour.a = 1;

        if (Math.Abs(Colour.a - 1.0f) > MathUtils.EPSILON)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compound colour cannot have alpha other than 1");
        }

        if (Math.Abs(Colour.r) < MathUtils.EPSILON &&
            Math.Abs(Colour.g) < MathUtils.EPSILON && Math.Abs(Colour.b) < MathUtils.EPSILON)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compound colour can't be black");
        }

        if (Volume <= 0)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Volume should be > 0");
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void Resolve()
    {
        LoadedIcon = GD.Load<Texture>(IconPath);

        if (IsCloud)
        {
            LoadedTexture = GD.Load<Texture>(TexturePath);
        }
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return Name;
    }
}
