﻿using System;
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
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    public float Volume;

    public bool IsCloud;

    /// <summary>
    ///   Scales the retention rate of this compound as a cloud in the environment
    /// </summary>
    public float DecayRate = 1.0f;

    /// <summary>
    ///   Whether this compound is an agent, i.e. synthesized for cell-to-cell interaction
    /// </summary>
    public bool IsAgent;

    public string IconPath = null!;

    /// <summary>
    ///   When this is true the compound is always considered to be useful and is not dumped.
    /// </summary>
    public bool IsAlwaysUseful;

    /// <summary>
    ///   Allows absorbing this compound from environmental clouds (also needs <see cref="IsCloud"/> to be true).
    ///   If false microbes can't absorb clouds of this compound type.
    /// </summary>
    public bool IsAbsorbable = true;

    public bool IsEnvironmental;

    /// <summary>
    ///   Unit for this compound, if applicable (e.g. °C for temperature)
    /// </summary>
    public string? Unit;

    /// <summary>
    ///   Whether this compound can be distributed in a colony
    /// </summary>
    public bool CanBeDistributed;

    /// <summary>
    ///   If true, this compound can be absorbed by microbes through intracellular digestion.
    /// </summary>
    public bool Digestible;

    public Color Colour;

    /// <summary>
    ///   Loaded icon for display in GUIs
    /// </summary>
    public Texture? LoadedIcon;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

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

        if (DecayRate > 1.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Decay rate can't be > 1");
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void Resolve()
    {
        LoadedIcon = GD.Load<Texture>(IconPath);
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
