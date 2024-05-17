using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Newtonsoft.Json;
using Saving.Serializers;

/// <summary>
///   Class that represents a species. This is an abstract base for
///   use by all stage-specific species classes.
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter($"Saving.Serializers.{nameof(ThriveTypeConverter)}")]
[JSONAlwaysDynamicType]
[UseThriveConverter]
[UseThriveSerializer]
public abstract class Species : ICloneable
{
    /// <summary>
    ///   This is not an auto property to make save compatibility easier
    /// </summary>
    [JsonProperty]
    private Dictionary<Compound, float>? cachedBaseReproductionCost;

    protected Species(uint id, string genus, string epithet)
    {
        ID = id;
        Genus = genus;
        Epithet = epithet;
    }

    /// <summary>
    ///   This is the amount of compounds cells of this type spawn with
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> InitialCompounds { get; private set; } = new();

    /// <summary>
    ///   The base compounds needed to reproduce an individual of this species. Do not modify the returned value.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<Compound, float> BaseReproductionCost =>
        cachedBaseReproductionCost ??= CalculateBaseReproductionCost();

    /// <summary>
    ///   Unique id of this species, used to identify this
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     In the previous version a string name was used to identify species, but it was just the word species
    ///     followed by a sequential number, so now this is an actual number.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public uint ID { get; private set; }

    public string Genus { get; set; }
    public string Epithet { get; set; }

    public Color Colour { get; set; } = new(1, 1, 1);

    /// <summary>
    ///   The colour value for GUI Components that want to show this species' colour.
    ///   This value has additional constraints compared to plain Colour,
    ///   for example ensuring full opacity to avoid transparency, which can cause rendering bugs.
    /// </summary>
    public Color GUIColour
    {
        get
        {
            var colour = Colour;
            return new Color(colour.R, colour.G, colour.B, 1);
        }
    }

    /// <summary>
    ///   Set to true when this species has evolved to a different species class type. This is mostly used to detect
    ///   that old species that should no longer be in use, are not used. Once this has been set to true, don't set
    ///   this back to false.
    /// </summary>
    public bool Obsolete { get; set; }

    /// <summary>
    ///   This holds all behavioural values and defines how this species will behave in the environment.
    /// </summary>
    [JsonProperty]
    public BehaviourDictionary Behaviour { get; set; } = new();

    /// <summary>
    ///   This is the global population (the sum of population in all patches)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Changing this has no effect as this is set after auto-evo from the per patch populations.
    ///   </para>
    /// </remarks>
    public long Population { get; set; } = 1;

    public int Generation { get; set; } = 1;

    /// <summary>
    ///   This is the genome of the species
    /// </summary>
    [JsonIgnore]
    public abstract string StringCode { get; }

    /// <summary>
    ///   When true this is the player species
    /// </summary>
    [JsonProperty]
    public bool PlayerSpecies { get; private set; }

    [JsonProperty]
    public EndosymbiosisData Endosymbiosis { get; private set; } = new();

    [JsonIgnore]
    public string FormattedName => Genus + " " + Epithet;

    /// <summary>
    ///   Returns <see cref="FormattedName"/> but includes bbcode tags for styling. Player's species will be emphasized
    ///   with bolding.
    /// </summary>
    [JsonIgnore]
    public string FormattedNameBbCode => PlayerSpecies ?
        $"[url=species:{ID}][b][i]{FormattedName}[/i][/b][/url]" :
        $"[url=species:{ID}][i]{FormattedName}[/i][/url]";

    [JsonIgnore]
    public string FormattedIdentifier => FormattedName + $" ({ID:n0})";

    [JsonIgnore]
    public bool IsExtinct => Population <= 0;

    /// <summary>
    ///   Triggered when this species is changed somehow. Should update any data that is cached in the species
    ///   regarding its properties, including <see cref="RepositionToOrigin"/>
    /// </summary>
    public virtual void OnEdited()
    {
        cachedBaseReproductionCost = null;
    }

    /// <summary>
    ///   Repositions the structure of the species according to stage specific rules
    /// </summary>
    /// <returns>True when repositioning happened, false if this was already positioned correctly</returns>
    public abstract bool RepositionToOrigin();

    public void SetPopulationFromPatches(long population)
    {
        if (population < 0)
        {
            Population = 0;
        }
        else
        {
            Population = population;
        }
    }

    /// <summary>
    ///   Immediate population change (from the player dying)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: THIS ASSUMPTION IS NOW INVALID. WE MAY HAVE A BUG. AUTO-EVO SPECIES NUMBERS NOW NEED EXTRA CACHING
    ///     especially probably affects things if auto-evo is not set to run while playing.
    ///     This should be made sure to not affect auto-evo. As long
    ///     as auto-evo uses the per patch population numbers this
    ///     doesn't affect that.
    ///   </para>
    ///   <para>
    ///     In addition to this an external population effect needs to be sent to auto-evo,
    ///     otherwise this effect disappears when auto-evo finishes.
    ///   </para>
    /// </remarks>
    public void ApplyImmediatePopulationChange(long constant, float coefficient, Patch patch)
    {
        ThrowPopulationChangeErrorIfNotPlayer();

        var oldPopulation = patch.GetSpeciesGameplayPopulation(this);
        var population = (long)(oldPopulation * coefficient);
        population += constant;

        if (population < 0)
            population = 0;

        patch.UpdateSpeciesGameplayPopulation(this, population);
    }

    /// <summary>
    ///   Apply properties from the mutation that are mutable
    /// </summary>
    public virtual void ApplyMutation(Species mutation)
    {
        InitialCompounds.Clear();

        foreach (var entry in mutation.InitialCompounds)
            InitialCompounds.Add(entry.Key, entry.Value);

        foreach (var entry in mutation.Behaviour)
            Behaviour[entry.Key] = entry.Value;

        Colour = mutation.Colour;

        cachedBaseReproductionCost = null;

        // These don't mutate for a species
        // genus;
        // epithet;
        // endosymbiosis
    }

    /// <summary>
    ///   Makes this the player species. This is a method as this is an important change
    /// </summary>
    public void BecomePlayerSpecies()
    {
        PlayerSpecies = true;
    }

    /// <summary>
    ///   Gets info specific to the species for storing into a new container class.
    ///   Used for patch snapshots, but could be expanded
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: Check overlap with ClonePropertiesTo
    ///   </para>
    /// </remarks>
    public SpeciesInfo RecordSpeciesInfo()
    {
        return new SpeciesInfo
        {
            ID = ID,
            Population = Population,
        };
    }

    /// <summary>
    ///   Only called by GameWorld when an externally created species is added to it. Should not be called from
    ///   anywhere else.
    /// </summary>
    /// <param name="newId">The new ID for this species for the new world</param>
    public void OnBecomePartOfWorld(uint newId)
    {
        ID = newId;
    }

    /// <summary>
    ///   Computes a set of initial compounds to spawn members of this species with based on what this species can
    ///   use
    /// </summary>
    public abstract void UpdateInitialCompounds();

    /// <summary>
    ///   Updates the name of this species if the new name is valid
    /// </summary>
    /// <param name="newName">The new name to try to switch to</param>
    public void UpdateNameIfValid(string newName)
    {
        var match = Regex.Match(newName, Constants.SPECIES_NAME_REGEX);
        if (match.Success)
        {
            Genus = match.Groups["genus"].Value;
            Epithet = match.Groups["epithet"].Value;

            GD.Print("Edited species name is now ", FormattedName);
        }
        else
        {
            GD.PrintErr("Invalid newName for species: ", newName);
        }
    }

    /// <summary>
    ///   Creates a cloned version of the species. This should only
    ///   really be used if you need to modify a species while
    ///   referring to the old data. In for example the Mutations
    ///   code.
    /// </summary>
    /// <returns>Deep-cloned instance of this object</returns>
    public abstract object Clone();

    /// <summary>
    ///   Instead of overriding the GetHashCode method this is provided to make checking
    ///   "is this species visually the same as this other one" possible by comparing the hashes.
    /// </summary>
    /// <returns>The visual hash code</returns>
    public virtual int GetVisualHashCode()
    {
        return Genus.GetHashCode() * 599 ^ Epithet.GetHashCode() * 601 ^ Colour.GetHashCode() * 607;
    }

    public virtual string GetDetailString()
    {
        // TODO: include endosymbiosis data?
        return Localization.Translate("SPECIES_DETAIL_TEXT").FormatSafe(FormattedNameBbCode,
            ID,
            Generation,
            Population,
            Colour.ToHtml(),
            string.Join("\n  ",
                Behaviour.Select(b => BehaviourDictionary.GetBehaviourLocalizedString(b.Key) + ": " + b.Value)));
    }

    public override string ToString()
    {
        return Obsolete ? "[OBSOLETE] " + FormattedIdentifier : FormattedIdentifier;
    }

    internal virtual void CopyDataToConvertedSpecies(Species species)
    {
        if (ID != species.ID)
            throw new ArgumentException("ID must be same in the target species (it needs to be a duplicated species)");

        foreach (var entry in Behaviour)
            species.Behaviour[entry.Key] = entry.Value;

        // Genus and epithet aren't copied as they are required constructor parameters
        species.Colour = Colour;
        species.Population = Population;
        species.Generation = Generation;
        species.PlayerSpecies = PlayerSpecies;

        // Preserve endosymbiosis progress object as the same, as this is meant to be used when converting species
        // types
        species.Endosymbiosis = Endosymbiosis;
    }

    /// <summary>
    ///   Helper for child classes to implement Clone
    /// </summary>
    internal void ClonePropertiesTo(Species species)
    {
        foreach (var entry in InitialCompounds)
            species.InitialCompounds[entry.Key] = entry.Value;

        foreach (var entry in Behaviour)
            species.Behaviour[entry.Key] = entry.Value;

        species.Endosymbiosis = Endosymbiosis.Clone();

        // Genus and epithet aren't copied as they are required constructor parameters
        species.Colour = Colour;
        species.Population = Population;
        species.Generation = Generation;
        species.ID = ID;

        // There can only be one player species at a time, so to avoid adding a method to reset this flag when
        // mutating, this property is just not copied
        // species.PlayerSpecies = PlayerSpecies;
    }

    protected virtual Dictionary<Compound, float> CalculateBaseReproductionCost()
    {
        var result = new Dictionary<Compound, float>();

        var simulationParameters = SimulationParameters.Instance;

        if (Constants.MICROBE_REPRODUCTION_COST_BASE_AMMONIA > 0)
        {
            result.Add(simulationParameters.GetCompound("ammonia"),
                Constants.MICROBE_REPRODUCTION_COST_BASE_AMMONIA);
        }

        if (Constants.MICROBE_REPRODUCTION_COST_BASE_PHOSPHATES > 0)
        {
            result.Add(simulationParameters.GetCompound("phosphates"),
                Constants.MICROBE_REPRODUCTION_COST_BASE_PHOSPHATES);
        }

        return result;
    }

    private void ThrowPopulationChangeErrorIfNotPlayer()
    {
        if (!PlayerSpecies)
            throw new InvalidOperationException("Cannot apply an immediate population change to an AI species");
    }
}
