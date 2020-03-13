using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Class that represents a species. This is an abstract base for
///   use by all stage-specific species classes.
/// </summary>
public abstract class Species
{
    /// <summary>
    ///   I *think* this is the amount of compounds members of this species consist of
    /// </summary>
    public readonly Dictionary<string, float> AvgCompoundAmounts =
        new Dictionary<string, float>();

    public string Genus;
    public string Epithet;

    public Color Colour = new Color(1, 1, 1);

    // Behavior properties
    public float Aggression = 100.0f;
    public float Opportunism = 100.0f;
    public float Fear = 100.0f;
    public float Activity = 0.0f;
    public float Focus = 0.0f;

    /// <summary>
    ///   This is the global population (the sum of population in all patches)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Changing this has no effect as this is set after auto-evo
    ///     from the per patch populations.
    ///   </para>
    /// </remarks>
    public int Population = 1;

    public int Generation = 1;

    protected Species(uint id)
    {
        ID = id;
    }

    /// <summary>
    ///   Unique id of this species, used to identity this
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     In the previous version a string name was used to identify
    ///     species, but it was just the word species followed by a
    ///     sequential number, so now this is an actual number.
    ///   </para>
    /// </remarks>
    public uint ID { get; private set; }

    /// <summary>
    ///   This is the genome of the species
    /// </summary>
    public string StringCode { get; set; }

    /// <summary>
    ///   When true this is the player species
    /// </summary>
    [JsonProperty]
    public bool PlayerSpecies { get; private set; } = false;

    public string FormattedName
    {
        get
        {
            return Genus + " " + Epithet;
        }
    }

    public string FormattedIdentifier
    {
        get
        {
            return FormattedName + string.Format(" ({0:n})", ID);
        }
    }

    public void
        SetPopulationFromPatches(int population)
    {
        if (population < 0)
        {
            this.Population = 0;
        }
        else
        {
            this.Population = population;
        }
    }

    /// <summary>
    ///   Immediate population change (from the player dying)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This should be made sure to not affect auto-evo. As long
    ///     as auto-evo uses the per patch population numbers this
    ///     doesn't affect that.
    ///   </para>
    ///   <para>
    ///     In addition to this an external population effect needs to
    ///     be sent to auto-evo, otherwise this effect disappears when
    ///     auto-evo finishes.
    ///   </para>
    /// </remarks>
    public void ApplyImmediatePopulationChange(int change)
    {
        Population += change;

        if (Population < 0)
            Population = 0;
    }

    /// <summary>
    ///   Makes this the player species. This is a method as this is an important change
    /// </summary>
    public void BecomePlayerSpecies()
    {
        PlayerSpecies = true;
    }
}
