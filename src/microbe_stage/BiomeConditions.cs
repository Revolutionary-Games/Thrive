using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   The conditions of a biome that can change. This is a separate class to make serialization work regarding the biome
/// </summary>
[UseThriveSerializer]
public class BiomeConditions : IBiomeConditions, ICloneable
{
    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> compounds;

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> currentCompoundAmounts;

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> averageCompoundAmounts;

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> maximumCompoundAmounts;

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> minimumCompoundAmounts;

    /// <summary>
    ///   Creates new biome conditions. The other compound amounts are nullable to allow
    ///   <see cref="SimulationParameters"/> to load this class.
    /// </summary>
    /// <param name="compounds">
    ///   The fallback compound amounts if specific compound amount category doesn't have a value
    /// </param>
    /// <param name="currentCompoundAmounts">Value for <see cref="CurrentCompoundAmounts"/></param>
    /// <param name="averageCompoundAmounts">Value for <see cref="AverageCompounds"/></param>
    /// <param name="maximumCompoundAmounts">Value for <see cref="MaximumCompounds"/></param>
    /// <param name="minimumCompoundAmounts">Value for <see cref="MinimumCompounds"/></param>
    [JsonConstructor]
    public BiomeConditions(Dictionary<Compound, BiomeCompoundProperties> compounds,
        Dictionary<Compound, BiomeCompoundProperties>? currentCompoundAmounts,
        Dictionary<Compound, BiomeCompoundProperties>? averageCompoundAmounts,
        Dictionary<Compound, BiomeCompoundProperties>? maximumCompoundAmounts,
        Dictionary<Compound, BiomeCompoundProperties>? minimumCompoundAmounts)
    {
        this.compounds = compounds;

        // Initialize the backing stores and the adapters that allow access. This is important to do just once to
        // save massively on the number of allocated objects.
        this.currentCompoundAmounts = currentCompoundAmounts ?? new Dictionary<Compound, BiomeCompoundProperties>();
        CurrentCompoundAmounts =
            new DictionaryWithFallback<Compound, BiomeCompoundProperties>(this.currentCompoundAmounts, compounds);

        this.averageCompoundAmounts = averageCompoundAmounts ?? new Dictionary<Compound, BiomeCompoundProperties>();
        AverageCompounds =
            new DictionaryWithFallback<Compound, BiomeCompoundProperties>(this.averageCompoundAmounts, compounds);

        this.maximumCompoundAmounts = maximumCompoundAmounts ?? new Dictionary<Compound, BiomeCompoundProperties>();
        MaximumCompounds =
            new DictionaryWithFallback<Compound, BiomeCompoundProperties>(this.maximumCompoundAmounts, compounds);

        this.minimumCompoundAmounts = minimumCompoundAmounts ?? new Dictionary<Compound, BiomeCompoundProperties>();
        MinimumCompounds =
            new DictionaryWithFallback<Compound, BiomeCompoundProperties>(this.minimumCompoundAmounts, compounds);
    }

    public Dictionary<string, ChunkConfiguration> Chunks { get; set; } = null!;

    /// <summary>
    ///   Environmental pressure in this biome (average value based on the average depth from the patch)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Default value is picked to work with default values in <see cref="EnvironmentalTolerances"/> so that this
    ///     won't totally blow up older saves.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public float Pressure { get; set; } = 101325;

    /// <summary>
    ///   The compound amounts that change in realtime during gameplay
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> CurrentCompoundAmounts { get; }

    /// <summary>
    ///   Average compounds over an in-game day
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> AverageCompounds { get; }

    /// <summary>
    ///   Maximum compounds this patch can reach. Not related to maximum during an in-game day, for that use
    ///   <see cref="Compounds"/>!
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> MaximumCompounds { get; }

    /// <summary>
    ///   Minimum compounds during an in-game day
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> MinimumCompounds { get; }

    /// <summary>
    ///   The normal, large timescale compound amounts. The maximum amounts compounds are at during a day in this
    ///   patch (if they wary during a day).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     I couldn't come up with a better name so this is named unimaginatively like this - hhyyrylainen
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyDictionary<Compound, BiomeCompoundProperties> Compounds => compounds;

    /// <summary>
    ///   Allows access to modification of the compound values in the biome permanently. Should only be used by
    ///   auto-evo or map generator. After changing <see cref="AverageCompounds"/> must to be updated.
    ///   <see cref="ModifyLongTermCondition"/> is the preferred method to update this data which handles that
    ///   automatically. Or for more advanced handling (of gases especially):
    ///   <see cref="ApplyLongTermCompoundChanges"/>
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> ChangeableCompounds => compounds;

    /// <summary>
    ///   Returns a new dictionary where <see cref="Compounds"/> is combined with compounds contained in
    ///   <see cref="Chunks"/>.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<Compound, BiomeCompoundProperties> CombinedCompounds
    {
        get
        {
            var result = new Dictionary<Compound, BiomeCompoundProperties>(compounds);

            foreach (var chunk in Chunks.Values)
            {
                if (chunk.Compounds == null)
                    continue;

                foreach (var compound in chunk.Compounds)
                {
                    compounds.TryGetValue(compound.Key, out BiomeCompoundProperties properties);
                    properties.Amount += compound.Value.Amount;
                    result[compound.Key] = properties;
                }
            }

            return result;
        }
    }

    public BiomeCompoundProperties GetCompound(Compound compound, CompoundAmountType amountType)
    {
        if (TryGetCompound(compound, amountType, out var result))
        {
            return result;
        }

        throw new KeyNotFoundException("Compound type not found in BiomeConditions");
    }

    public bool TryGetCompound(Compound compound, CompoundAmountType amountType, out BiomeCompoundProperties result)
    {
        switch (amountType)
        {
            case CompoundAmountType.Current:
                return CurrentCompoundAmounts.TryGetValue(compound, out result);
            case CompoundAmountType.Maximum:
                return MaximumCompounds.TryGetValue(compound, out result);
            case CompoundAmountType.Minimum:
                return MinimumCompounds.TryGetValue(compound, out result);
            case CompoundAmountType.Average:
                return AverageCompounds.TryGetValue(compound, out result);
            case CompoundAmountType.Biome:
                return Compounds.TryGetValue(compound, out result);
            case CompoundAmountType.Template:
                throw new NotSupportedException("BiomeConditions doesn't have access to template");
            default:
                throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null);
        }
    }

    /// <summary>
    ///   Applies a set of compound value changes all at once. Handles environmental compound more intelligently than
    ///   pure <see cref="ModifyLongTermCondition"/>. Note should be only called for long-timescale operations like
    ///   <see cref="IWorldEffect"/> (and also this allocates some temporary work memory)
    /// </summary>
    /// <param name="biomeDetails">
    ///   Needed details about the current patch, needed to know some physical properties of it for the gas handling
    /// </param>
    /// <param name="changes">
    ///   The changes to apply. Changes that would negative compounds are clamped automatically
    /// </param>
    /// <param name="newCloudSizes">
    ///   Specifies the compound cloud spawn sizes for all new non-environmental compounds
    /// </param>
    public void ApplyLongTermCompoundChanges(Biome biomeDetails, Dictionary<Compound, float> changes,
        IReadOnlyDictionary<Compound, float> newCloudSizes)
    {
        var simulationParameters = SimulationParameters.Instance;

        // Apply first non-environmental types as that is the easiest
        foreach (var entry in changes)
        {
            var definition = simulationParameters.GetCompoundDefinition(entry.Key);
            if (!definition.IsEnvironmental)
            {
                if (!TryGetCompound(entry.Key, CompoundAmountType.Biome, out var existing))
                {
                    if (!newCloudSizes.TryGetValue(entry.Key, out var cloudSize))
                    {
                        GD.PrintErr(
                            $"Unknown cloud spawn size to use for {entry.Key}, using a default hardcoded value");
                        cloudSize = 250000;
                    }

                    existing.Amount = cloudSize;
                }

                existing.Density += entry.Value;
                ModifyLongTermCondition(entry.Key, existing);
            }
            else if (!definition.IsGas)
            {
                // Then non-gas environmental can also be applied here as these don't use custom handling
                TryGetCompound(entry.Key, CompoundAmountType.Biome, out var existing);

                existing.Ambient = Math.Clamp(existing.Ambient + entry.Value, 0, 1);
                ModifyLongTermCondition(entry.Key, existing);
            }
        }

        // Then apply environmental all at once as absolute values so that the gas percentages will not add up to over
        // 100%

        // Calculate current gases in absolute volume
        var gases = new Dictionary<Compound, float>();
        float previousTotal = 0;

        foreach (var current in compounds)
        {
            if (simulationParameters.GetCompoundDefinition(current.Key).IsGas)
            {
                var absoluteAmount = biomeDetails.GasVolume * current.Value.Ambient;
                gases[current.Key] = absoluteAmount;
                previousTotal += absoluteAmount;
            }
        }

        float previousOther = 1 * biomeDetails.GasVolume - previousTotal;

        foreach (var change in changes)
        {
            if (!simulationParameters.GetCompoundDefinition(change.Key).IsGas)
                continue;

            gases.TryGetValue(change.Key, out var updatedValue);

            updatedValue = Math.Clamp(updatedValue + change.Value, 0, biomeDetails.GasVolume);

            gases[change.Key] = updatedValue;
        }

        float totalGases = 0;
        foreach (var pair in gases)
        {
            totalGases += pair.Value;
        }

        // Add some other compounds filling up stuff, but gradually remove them as other compounds build up
        // The slightly above one multiplier here is to make this a bit more often to trigger
        if (totalGases > previousTotal - previousOther * 1.01f)
        {
            previousOther *= 1 - Constants.OTHER_GASES_DECAY_SPEED;
        }

        if (previousOther > MathUtils.EPSILON)
            totalGases += previousOther;

        // Finally scale each compound by its fraction of the total and apply it
        foreach (var gas in gases)
        {
            TryGetCompound(gas.Key, CompoundAmountType.Biome, out var result);

            // Safety for when there are *no* gases
            if (totalGases < MathUtils.EPSILON)
            {
                result.Ambient = 0;
            }
            else
            {
                result.Ambient = gas.Value / totalGases;
            }

            ModifyLongTermCondition(gas.Key, result);
        }
    }

    /// <summary>
    ///   Modifies the long-term amount of a compound in this biome. This is preferable to directly modifying
    ///   <see cref="ChangeableCompounds"/>. This is only usable for compounds that don't vary along an in-game day.
    /// </summary>
    /// <param name="compound">The compound to modify</param>
    /// <param name="newValue">New value to set</param>
    public void ModifyLongTermCondition(Compound compound, BiomeCompoundProperties newValue)
    {
        // Ensure negative values can't be calculated and applied accidentally. This is here as conditions are modified
        // from many places so the easiest and safes thing is to just clamp stuff to non-zero here
        if (newValue.Ambient < 0)
            newValue.Ambient = 0;

        if (newValue.Density < 0)
            newValue.Density = 0;

        // As this is the cloud size, this is unlikely to be wrong, but probably better to check here than to cause
        // weird issues in cloud spawning
        if (newValue.Amount < 0)
            newValue.Amount = 0;

        ChangeableCompounds[compound] = newValue;

        // Reset other related values
        AverageCompounds[compound] = newValue;

        // This is only fine to do on compounds that don't vary along a day
        CurrentCompoundAmounts[compound] = newValue;
    }

    public IEnumerable<Compound> GetAmbientCompoundsThatVary()
    {
        const float epsilon = 0.000001f;

        foreach (var minimumCompound in MinimumCompounds)
        {
            if (!Compounds.TryGetValue(minimumCompound.Key, out var maxValue) ||
                Math.Abs(maxValue.Ambient - minimumCompound.Value.Ambient) > epsilon)
            {
                yield return minimumCompound.Key;
            }
        }
    }

    public bool HasCompoundsThatVary()
    {
        const float epsilon = 0.000001f;

        foreach (var minimumCompound in MinimumCompounds)
        {
            if (!Compounds.TryGetValue(minimumCompound.Key, out var maxValue) ||
                Math.Abs(maxValue.Ambient - minimumCompound.Value.Ambient) > epsilon)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsVaryingCompound(Compound compound)
    {
        const float epsilon = 0.000001f;

        bool hasNormally = Compounds.TryGetValue(compound, out var normal);
        bool hasMinimum = MinimumCompounds.TryGetValue(compound, out var minimum);

        // Not varying if the numbers are the same
        if (!hasNormally && !hasMinimum)
            return false;

        if (hasNormally && hasMinimum && Math.Abs(normal.Ambient - minimum.Ambient) < epsilon)
            return false;

        return true;
    }

    public float CalculateOxygenResistanceFactor()
    {
        // TODO: maybe would be nicer to have some kind of exponential or other non-linear relationship here?
        var oxygen = Math.Clamp(GetCompound(Compound.Oxygen, CompoundAmountType.Biome).Ambient, 0, 1);

        if (oxygen <= Constants.TOLERANCE_OXYGEN_APPLY_AFTER)
            return 0;

        return oxygen;
    }

    public float CalculateUVFactor()
    {
        // Assume it is directly related to sunlight
        var light = Math.Clamp(GetCompound(Compound.Sunlight, CompoundAmountType.Biome).Ambient, 0, 1);

        if (light <= Constants.TOLERANCE_UV_APPLY_AFTER)
            return 0;

        return light;
    }

    public void Check(string name)
    {
        if (compounds == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compounds missing");
        }

        if (Chunks == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Chunks missing");
        }

        float sumOfGasses = 0;

        foreach (var compound in compounds)
        {
            if (compound.Value.Density * Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR is < 0 or > 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Density {compound.Value.Density} invalid for {compound.Key} " +
                    $"(scale factor is {Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR})");
            }

            if (compound.Value.Ambient > 0 && SimulationParameters.Instance.GetCompoundDefinition(compound.Key).IsGas)
            {
                sumOfGasses += compound.Value.Ambient;
            }
        }

        if (sumOfGasses > 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Gas compounds shouldn't together be over 100%");
        }

        foreach (var chunk in Chunks)
        {
            if (chunk.Value.Density * Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR is < 0 or > 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Density {chunk.Value.Density} invalid for {chunk.Key} " +
                    $"(scale factor is {Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR})");
            }

            if (string.IsNullOrEmpty(chunk.Value.Name))
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Missing name for chunk type");
            }

            if (chunk.Value.PhysicsDensity <= 0)
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Missing physics density for chunk type");
            }
        }
    }

    public object Clone()
    {
        // Shallow cloning is enough here thanks to us using value types (structs) as the dictionary values
        var result = new BiomeConditions(compounds.CloneShallow(), currentCompoundAmounts.CloneShallow(),
            averageCompoundAmounts.CloneShallow(), maximumCompoundAmounts.CloneShallow(),
            minimumCompoundAmounts.CloneShallow())
        {
            Chunks = Chunks.CloneShallow(),
            Pressure = Pressure,
        };

        return result;
    }
}
