// Tradeoff between safety and faster score lookups

#define USE_HASHED_SCORE_KEYS

// Some extra debug stuff that should be disabled in most cases
// #define VERIFY_PROCESS_SPEED_CACHE_RETURNS

// If set, enables checking whether GetHashCode causes serious duplicate cache value sharing problems
// This define is file-local. To cover predation scoring, uncomment it in both SimulationCache.cs and
// SimulationCache.PredationScoring.cs.
// This uses a ton of extra memory and time, so only enable it while debugging hash reuse.
// #define CHECK_HASH_CODE_REUSED_INSTANCES

// Turning this off has false positives due to OnEdited being called after first caching, so the visual hash will change
// due to re-centering of the cell layout
#define CHECK_CACHE_STORE_INSTANCES

namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;
using Systems;

/// <summary>
///   Caches some information in auto-evo runs to speed them up
/// </summary>
/// <remarks>
///   <para>
///     Some information will get outdated when data that the auto-evo relies on changes. If in the future
///     caching is moved to a higher level in the auto-evo, that needs to be considered.
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     TODO: would be better to reuse instances of this class after clearing them for next use (there's now a Clear
///     method for this future use case). See: https://github.com/Revolutionary-Games/Thrive/issues/6664
///   </para>
/// </remarks>
public partial class SimulationCache
{
    private readonly CompoundDefinition mucilage = SimulationParameters.GetCompound(Compound.Mucilage);

    private readonly WorldGenerationSettings worldSettings;
    private readonly PredationScoring predationScoring;

#if USE_HASHED_SCORE_KEYS
    private readonly Dictionary<ulong, float> cachedPressureScores = new();

    private readonly Dictionary<ulong, EnergyBalanceInfoSimple> cachedSimpleEnergyBalances = [];
#else
    private readonly Dictionary<(int, SelectionPressure, Patch), float> cachedPressureScores = new();

    private readonly Dictionary<(int, IBiomeConditions), EnergyBalanceInfoSimple>
        cachedSimpleEnergyBalances = [];
#endif

    private readonly Dictionary<int, float> cachedBaseSpeeds = new();
    private readonly Dictionary<int, float> cachedBaseHexSizes = new();

    private readonly Dictionary<ulong, ProcessSpeedInformation> cachedProcessSpeeds = new();

    private readonly Dictionary<int, List<TweakedProcess>> cachedProcessLists = new();

    private readonly Dictionary<(int, BiomeConditions), bool> cachedUsesVaryingCompounds = new();

    private readonly Dictionary<OrganelleDefinition, int> workMemory1 = new();

#if CHECK_HASH_CODE_REUSED_INSTANCES
    /// <summary>
    ///   Used to check if GetHashCode returns the same value that the species is likely the same. If not, then this can
    ///   eventually catch duplicate hash problems.
    /// </summary>
#if CHECK_CACHE_STORE_INSTANCES
    private readonly Dictionary<int, MicrobeSpecies> microbeHashCheckValues = new();
#else
    private readonly Dictionary<int, ulong> microbeHashCheckValues = new();
#endif
#endif

    public SimulationCache(WorldGenerationSettings worldSettings)
    {
        this.worldSettings = worldSettings;
        predationScoring = new PredationScoring(this);
    }

    public float GetPressureScore(SelectionPressure pressure, Patch patch, Species species)
    {
#if USE_HASHED_SCORE_KEYS

        // TODO: even better would be if pressure scores had unique IDs and we could use the species ID +
        // some modification marker as the hash input here
        var key = (ulong)(uint)pressure.GetHashCode() << 32 | (uint)GetSpeciesCacheKey(species);

        // Use a big prime to shuffle the hash to hopefully avoid collisions
        key *= 11265003396083139817;

        key += (ulong)patch.ID;

        key *= 13900709095051265681;
#else
        var key = (GetSpeciesCacheKey(species), pressure, patch);
#endif

        ref var score = ref CollectionsMarshal.GetValueRefOrNullRef(cachedPressureScores, key);
        if (!Unsafe.IsNullRef(ref score))
        {
            return score;
        }

        var cached = pressure.Score(species, patch, this);

        cachedPressureScores.Add(key, cached);
        return cached;
    }

    /// <summary>
    ///   Calculates the full energy balance for the given species in the given biome conditions.
    ///   Only accepts Microbe and Multicellular Species
    /// </summary>
    public EnergyBalanceInfoSimple GetEnergyBalanceForSpecies(Species species,
        BiomeConditions biomeConditions)
    {
        // TODO: this gets called an absolute ton with the new auto-evo so a more efficient caching method (to allow
        // different species but with same organelles to be able to use the same cache value) would be nice here

#if USE_HASHED_SCORE_KEYS
        var key = (ulong)(uint)biomeConditions.GetHashCode() << 32 | (uint)GetSpeciesCacheKey(species);
#else
        var key = (GetSpeciesCacheKey(species), biomeConditions);
#endif
        ref var balance = ref CollectionsMarshal.GetValueRefOrNullRef(cachedSimpleEnergyBalances, key);
        if (!Unsafe.IsNullRef(ref balance))
        {
            return balance;
        }

        var cached = new EnergyBalanceInfoSimple();

        var environmentalTolerances = GetEnvironmentalTolerances(species, biomeConditions);
        if (species is MicrobeSpecies microbeSpecies)
        {
            var maximumMovementDirection =
                MicrobeInternalCalculations.MaximumSpeedDirection(microbeSpecies.Organelles);
            var totalSpecializationBonus = microbeSpecies.CellTypeSpecializationBonus;

            // Auto-evo uses the average values of compound during the course of a simulated day
            ProcessSystem.ComputeEnergyBalanceSimple(microbeSpecies.Organelles, biomeConditions,
                environmentalTolerances, totalSpecializationBonus, microbeSpecies.MembraneType,
                maximumMovementDirection, true, species.PlayerSpecies, worldSettings,
                CompoundAmountType.Average, this, cached);
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            // Currently, ComputeEnergyBalanceSimple is not set up to safely add onto existing energy balances,
            // so we need to add up from a temporary balance per cell.
            var cellBalance = new EnergyBalanceInfoSimple();

            var cellTypes = multicellularSpecies.CellTypes;
            for (var i = 0; i < cellTypes.Count; ++i)
            {
                var cellType = cellTypes[i];

                // Perhaps this should instead take a MaximumSpeedDirection of the organism as a whole?
                var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(cellType.Organelles);

                var cellTypeSpecializationBonus = cellType.CellTypeSpecializationBonus;

                foreach (var hex in multicellularSpecies.EditorCells)
                {
                    if (hex.Data == null)
                        throw new ArgumentException("editor cell does not have celltemplate set");

                    var cell = hex.Data;

                    if (!ReferenceEquals(cell.CellType, cellType))
                        continue;

                    cellBalance.Clear();

                    var totalSpecializationBonus = cellTypeSpecializationBonus *
                        CellBodyPlanInternalCalculations.GetAdjacencySpecializationBonusFromBodyPlan(cell,
                            multicellularSpecies.EditorCells);

                    // Auto-evo uses the average values of compound during the course of a simulated day
                    ProcessSystem.ComputeEnergyBalanceSimple(cellType.Organelles, biomeConditions,
                        environmentalTolerances, totalSpecializationBonus, cellType.MembraneType,
                        maximumMovementDirection, true, species.PlayerSpecies, worldSettings,
                        CompoundAmountType.Average, this, cellBalance);

                    cached.Add(cellBalance);
                }
            }
        }
        else
        {
            throw new ArgumentException("Incompatible species type given");
        }

        cachedSimpleEnergyBalances.Add(key, cached);
        return cached;
    }

    public EnergyBalanceInfoSimple GetEnergyBalanceForCellType(IReadOnlyCellTypeDefinition celltype,
        MulticellularSpecies species, BiomeConditions biomeConditions)
    {
        var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(celltype.Organelles);

        // TODO: check if caching instances of these objects would be better than always recreating
        var cached = new EnergyBalanceInfoSimple();

        var totalSpecializationBonus = celltype.CellTypeSpecializationBonus;

        // Auto-evo uses the average values of compound during the course of a simulated day
        ProcessSystem.ComputeEnergyBalanceSimple(celltype.Organelles, biomeConditions,
            GetEnvironmentalTolerances(species, biomeConditions), totalSpecializationBonus, celltype.MembraneType,
            maximumMovementDirection, true, species.PlayerSpecies, worldSettings, CompoundAmountType.Average, this,
            cached);

        return cached;
    }

    // TODO: Both of these seem like something that could easily be stored on the species with OnEdited
    // And also *not* caching them at all is much slower (so if not cached in species, they must be cached here)
    public float GetSpeedForSpecies(Species species)
    {
#if CHECK_HASH_CODE_REUSED_INSTANCES
        CheckSpecies(species);
#endif

        var key = GetSpeciesCacheKey(species);

        ref var speed = ref CollectionsMarshal.GetValueRefOrNullRef(cachedBaseSpeeds, key);
        if (!Unsafe.IsNullRef(ref speed))
        {
            return speed;
        }

        float cached;
        if (species is MicrobeSpecies microbeSpecies)
        {
            var organelles = microbeSpecies.Organelles;

            // For MicrobeSpecies, Cell Type Specialization = Total Specialization Bonus
            var totalSpecializationBonus = microbeSpecies.CellTypeSpecializationBonus;

            cached = MicrobeInternalCalculations.CalculateSpeed(organelles.Organelles, microbeSpecies.MembraneType,
                microbeSpecies.MembraneRigidity, microbeSpecies.IsBacteria, totalSpecializationBonus, true);
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            cached = CellBodyPlanInternalCalculations.CalculateSpeed(multicellularSpecies.ModifiableEditorCells);
        }
        else
        {
            throw new ArgumentException("Incompatible species type given");
        }

        cachedBaseSpeeds.Add(key, cached);
        return cached;
    }

    public float GetBaseHexSizeForSpecies(Species species)
    {
#if CHECK_HASH_CODE_REUSED_INSTANCES
        CheckSpecies(species);
#endif

        var key = GetSpeciesCacheKey(species);

        ref var size = ref CollectionsMarshal.GetValueRefOrNullRef(cachedBaseHexSizes, key);
        if (!Unsafe.IsNullRef(ref size))
        {
            return size;
        }

        float cached;
        if (species is MicrobeSpecies microbeSpecies)
        {
            cached = microbeSpecies.BaseHexSize;
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            cached = multicellularSpecies.BaseHexSize;
        }
        else
        {
            throw new ArgumentException("Incompatible species type given");
        }

        cachedBaseHexSizes.Add(key, cached);
        return cached;
    }

    public float GetBaseHexSizeForCellType(IReadOnlyCellTypeDefinition cellType)
    {
        // Not yet profiled to decide whether this should be cached or not

        return cellType.BaseHexSize;
    }

    public float GetRotationSpeedForSpecies(Species species)
    {
        // TODO: this might be useful to cache though this is just used from a single place (though targeted
        // prey species by multiple predators might benefit ever so slightly, but it seems kind of unlikely).
        // A more useful thing would be to cache this directly in the species when calculating other movement cached
        // properties.
        if (species is MicrobeSpecies microbeSpecies)
        {
            var organelles = microbeSpecies.Organelles;

            // For MicrobeSpecies, Cell Type Specialization = Total Specialization Bonus
            var totalSpecializationBonus = microbeSpecies.CellTypeSpecializationBonus;

            return MicrobeInternalCalculations.CalculateRotationSpeed(organelles.Organelles, totalSpecializationBonus);
        }

        if (species is MulticellularSpecies multicellularSpecies)
        {
            return CellBodyPlanInternalCalculations.CalculateRotationSpeed(multicellularSpecies.ModifiableEditorCells);
        }

        throw new ArgumentException("Incompatible species type given");
    }

    public float GetCompoundConversionScoreForSpecies(CompoundDefinition fromCompound, CompoundDefinition toCompound,
        Species species)
    {
        // This method was faster (for MicrobeSpecies) when not using caching (not tested for MulticellularSpecies)
        // With cache: 3 925 ms for 1,470 million calls
        // Without caching: 2 284 ms for 1,291 million calls

        var compoundIn = 0.0f;
        var compoundOut = 0.0f;
        var activeProcessList = GetActiveProcessList(species);

        // For maximum efficiency, as this is called an absolute ton, the following approach is used
        foreach (var process in activeProcessList)
        {
            if (process.Process.Inputs.TryGetValue(fromCompound, out var inputAmount))
            {
                if (process.Process.Outputs.TryGetValue(toCompound, out var outputAmount))
                {
                    // We don't multiply by speed here as it is about pure efficiency
                    compoundIn += inputAmount;
                    compoundOut += outputAmount;
                }
            }
        }

        float cached;
        if (compoundIn <= 0)
        {
            cached = 0;
        }
        else
        {
            cached = compoundOut / compoundIn;
        }

        return cached;
    }

    public float GetCompoundGeneratedFrom(CompoundDefinition fromCompound, CompoundDefinition toCompound,
        Species species, BiomeConditions biomeConditions)
    {
        // This method was faster for microbe species when not using caching
        // With cache: 2 408 ms for 776 344 calls
        // Without caching: 1 257 ms for 680 411 calls

        var cached = 0.0f;

        var activeProcessList = GetActiveProcessList(species);

        var tolerances = GetEnvironmentalTolerances(species, biomeConditions);

        foreach (var process in activeProcessList)
        {
            if (process.Process.Inputs.ContainsKey(fromCompound))
            {
                if (process.Process.Outputs.TryGetValue(toCompound, out var outputAmount))
                {
                    var processSpeed =
                        GetProcessMaximumSpeed(process, tolerances.ProcessSpeedModifier, biomeConditions)
                            .CurrentSpeed;

                    cached += outputAmount * processSpeed;
                }
            }
        }

        return cached;
    }

    /// <summary>
    ///   Calculates a maximum speed for a process that can happen given the environmental. Environmental compounds
    ///   are always used at the average amount in auto-evo.
    /// </summary>
    /// <param name="process">The process to calculate the speed for</param>
    /// <param name="speedModifier">
    ///   Process speed modifier from <see cref="ResolvedMicrobeTolerances.ProcessSpeedModifier"/>
    /// </param>
    /// <param name="biomeConditions">The biome conditions to use</param>
    /// <returns>The speed information for the process</returns>
    /// <remarks>
    ///   <para>
    ///     This is important to cache as it is called very many times, but the speed modifier slightly reduces
    ///     the cache usefulness.
    ///   </para>
    /// </remarks>
    public ProcessSpeedInformation GetProcessMaximumSpeed(TweakedProcess process, float speedModifier,
        IBiomeConditions biomeConditions)
    {
        // For caching resolve some data already to have better cache hits
        var effectiveMultiplier = process.Rate * speedModifier;

        // 16 low bits of the key (as process amounts are limited, we save bits on them)
        ulong key = process.Process.ProcessId;

        // These slightly overlap, but hopefully this doesn't lead to collisions (the most significant effect would be
        // just a process or two running at the wrong speed)
        // The overlap is 16 bits of the upper end of the float
        key |= (ulong)(uint)BitConverter.SingleToInt32Bits(effectiveMultiplier) << 16;
        key ^= (ulong)(uint)biomeConditions.GetHashCode() << 32;

        // Shuffle key bits with a prime number (we could do a double shuffle above, but processes are needed so much
        // that we do not want the extra work)
        key *= 9853659385249210933;

        ref var speed = ref CollectionsMarshal.GetValueRefOrNullRef(cachedProcessSpeeds, key);
        if (!Unsafe.IsNullRef(ref speed))
        {
#if VERIFY_PROCESS_SPEED_CACHE_RETURNS
            if (speed.Process != process.Process)
                throw new Exception("Cached process speed does not match requested process");
#endif

            return speed;
        }

        // TODO: cache process speed information objects?
        var cached = ProcessSystem.CalculateProcessMaximumSpeed(process, speedModifier, biomeConditions,
            CompoundAmountType.Average, true);

        cachedProcessSpeeds.Add(key, cached);
        return cached;
    }

    public float GetPredationScore(Species predatorSpecies, Species preySpecies, BiomeConditions biomeConditions)
    {
        return predationScoring.GetScore(predatorSpecies, preySpecies, biomeConditions);
    }

    public bool GetUsesVaryingCompoundsForSpecies(Species species, BiomeConditions biomeConditions)
    {
#if CHECK_HASH_CODE_REUSED_INSTANCES
        CheckSpecies(species);
#endif

        // Disabling this cache makes this ever so slightly slower
        var key = (GetSpeciesCacheKey(species), biomeConditions);

        ref var usesVarying = ref CollectionsMarshal.GetValueRefOrNullRef(cachedUsesVaryingCompounds, key);
        if (!Unsafe.IsNullRef(ref usesVarying))
        {
            return usesVarying;
        }

        var cached = false;
        if (species is MicrobeSpecies microbeSpecies)
        {
            cached = MicrobeInternalCalculations.UsesDayVaryingCompounds(microbeSpecies.Organelles, biomeConditions,
                null);
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            foreach (var hex in multicellularSpecies.EditorCells)
            {
                var cell = hex.Data;
                if (cell != null)
                {
                    if (cached)
                        break;

                    cached = MicrobeInternalCalculations.UsesDayVaryingCompounds(cell.Organelles,
                        biomeConditions, null);
                }
            }
        }
        else
        {
            throw new ArgumentException("Incompatible species type given");
        }

        cachedUsesVaryingCompounds.Add(key, cached);
        return cached;
    }

    public float GetChemoreceptorCloudScore(Species species, CompoundDefinition compound,
        BiomeConditions biomeConditions)
    {
        // This method was for microbe species faster when not using caching
        // Measurement for microbe species only:
        // With cache: 2 192 ms for 1,245 million calls
        // Without caching: 762 ms for 1,096 million calls

        var cached = 0.0f;

        // Need to have chemoreceptor to be able to "smell" clouds
        var hasChemoreceptor = false;
        if (species is MicrobeSpecies microbeSpecies)
        {
            var organelles = microbeSpecies.Organelles.Organelles;
            for (var i = 0; i < organelles.Count; ++i)
            {
                var organelle = organelles[i];

                var organelleTargetCompound = organelle.GetActiveTargetCompound();
                if (organelleTargetCompound == Compound.Invalid)
                    continue;

                if (organelleTargetCompound == compound.ID)
                    hasChemoreceptor = true;
            }
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            foreach (var hex in multicellularSpecies.EditorCells)
            {
                if (hasChemoreceptor)
                    break;

                var cell = hex.Data;
                if (cell != null)
                {
                    foreach (var organelle in cell.CellType.Organelles)
                    {
                        var organelleTargetCompound = organelle.GetActiveTargetCompound();
                        if (organelleTargetCompound == Compound.Invalid)
                            continue;

                        if (organelleTargetCompound == compound.ID)
                        {
                            hasChemoreceptor = true;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            throw new ArgumentException("Incompatible species type given");
        }

        if (hasChemoreceptor)
        {
            if (biomeConditions.AverageCompounds.TryGetValue(compound.ID, out var compoundData) &&
                compoundData.Density > 0)
            {
                cached = Constants.AUTO_EVO_CHEMORECEPTOR_BASE_SCORE
                    + Constants.AUTO_EVO_CHEMORECEPTOR_VARIABLE_CLOUD_SCORE
                    / (compoundData.Density * compoundData.Amount);
            }
        }

        return cached;
    }

    public float GetChemoreceptorChunkScore(Species species, ChunkConfiguration chunk,
        CompoundDefinition compound)
    {
        // This method is faster when not using caching
        // With cache: 3 977 ms for 2,005 million calls
        // Without caching: 916 ms for 1,285 million calls

        var cached = 0.0f;

        // If the chunk doesn't spawn, it doesn't give any of its compound
        if (chunk.Density <= 0)
            return cached;

        // Need to have chemoreceptor to be able to "smell" chunks
        var hasChemoreceptor = false;
        if (species is MicrobeSpecies microbeSpecies)
        {
            var organelles = microbeSpecies.Organelles.Organelles;
            for (var i = 0; i < organelles.Count; ++i)
            {
                var organelle = organelles[i];

                var organelleTargetCompound = organelle.GetActiveTargetCompound();
                if (organelleTargetCompound == Compound.Invalid)
                    continue;

                if (organelleTargetCompound == compound.ID)
                    hasChemoreceptor = true;
            }
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            foreach (var hex in multicellularSpecies.EditorCells)
            {
                if (hasChemoreceptor)
                    break;

                var cell = hex.Data;
                if (cell != null)
                {
                    foreach (var organelle in cell.CellType.Organelles)
                    {
                        var organelleTargetCompound = organelle.GetActiveTargetCompound();
                        if (organelleTargetCompound == Compound.Invalid)
                            continue;

                        if (organelleTargetCompound == compound.ID)
                        {
                            hasChemoreceptor = true;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            throw new ArgumentException("Incompatible species type given");
        }

        // If the chunk doesn't spawn, it doesn't give any of its compound
        if (hasChemoreceptor && chunk.Density > 0)
        {
            // We use null suppression here
            // as this method is only meant to be called on chunks that are known to contain the given compound
            if (!chunk.Compounds!.TryGetValue(compound.ID, out var compoundAmount))
                throw new ArgumentException("Chunk does not contain compound");

            cached = Constants.AUTO_EVO_CHEMORECEPTOR_BASE_SCORE
                + Constants.AUTO_EVO_CHEMORECEPTOR_VARIABLE_CHUNK_SCORE
                / (chunk.Density * MathF.Pow(compoundAmount.Amount, Constants.AUTO_EVO_CHUNK_AMOUNT_NERF));
        }

        return cached;
    }

    public bool MatchesSettings(WorldGenerationSettings checkAgainst)
    {
        return worldSettings.Equals(checkAgainst);
    }

    /// <summary>
    ///   Clears all data in this cache. Can be used to re-use a cache object *but should not be called* while anything
    ///   might still be using this cache currently!
    /// </summary>
    public void Clear()
    {
        cachedPressureScores.Clear();
        cachedSimpleEnergyBalances.Clear();
        cachedBaseSpeeds.Clear();
        cachedBaseHexSizes.Clear();
        cachedProcessSpeeds.Clear();
        predationScoring.Clear();
        cachedUsesVaryingCompounds.Clear();
        cachedProcessLists.Clear();
    }

    public List<TweakedProcess> GetActiveProcessList(Species species)
    {
#if CHECK_HASH_CODE_REUSED_INSTANCES
        CheckSpecies(species);
#endif

        var key = GetSpeciesCacheKey(species);
        if (cachedProcessLists.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // TODO: a buffer of process lists (to make small list allocations rarer) (as cached is null here if not found)
        if (species is MicrobeSpecies microbeSpecies)
        {
            ProcessSystem.ComputeActiveProcessList(microbeSpecies.Organelles, ref cached);
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            List<IReadOnlyOrganelleTemplate> allOrganelles = [];

            foreach (var cell in multicellularSpecies.EditorCells)
            {
                foreach (var organelle in cell.Data!.CellType.Organelles)
                {
                    allOrganelles.Add(organelle);
                }
            }

            ProcessSystem.ComputeActiveProcessList(allOrganelles, ref cached);
        }
        else
        {
            throw new ArgumentException("Incompatible species type given");
        }

        cachedProcessLists.Add(key, cached);
        return cached;
    }

    public float GetEnzymesScore(MulticellularSpecies multicellularSpecies, string dissolverEnzyme, float preyHexSize,
        float enzymesScore)
    {
        var cellTypes = multicellularSpecies.CellTypes;
        for (var i = 0; i < cellTypes.Count; ++i)
        {
            var cellType = cellTypes[i];
            if (!cellType.MembraneType.CanEngulf)
                continue;

            var cellTypeHexSize = GetBaseHexSizeForCellType(cellType);
            if (cellTypeHexSize / preyHexSize <= Constants.ENGULF_SIZE_RATIO_REQ)
                continue;

            var cellTypeSpecializationBonus = cellType.CellTypeSpecializationBonus;
            var cells = multicellularSpecies.EditorCells;

            foreach (var hex in cells)
            {
                var cell = hex.Data;
                if (cell != null && ReferenceEquals(cell.CellType, cellType))
                {
                    var cellEnzymesScore = GetEnzymesScore(cellType, dissolverEnzyme,
                        cellTypeSpecializationBonus * CellBodyPlanInternalCalculations
                            .GetAdjacencySpecializationBonusFromBodyPlan(cell, cells));
                    if (cellEnzymesScore > enzymesScore)
                        enzymesScore = cellEnzymesScore;
                }
            }
        }

        return enzymesScore;
    }

    public ResolvedMicrobeTolerances GetEnvironmentalTolerances(Species species,
        BiomeConditions biomeConditions)
    {
        // This method is faster when not using caching
        // With cache: 1 692 ms for 1,882 million calls
        // Without caching: 132 ms for 2,095 million calls
        // Not yet known whether this is faster with or without caching for MulticellularSpecies

        if (species is MicrobeSpecies microbeSpecies)
        {
            var tolerances =
                MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(microbeSpecies, biomeConditions);

            return MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances);
        }

        if (species is MulticellularSpecies multicellularSpecies)
        {
            var tolerances =
                MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(multicellularSpecies, biomeConditions);

            return MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances);
        }

        throw new ArgumentException("Incompatible species type given");
    }

    public PredationToolsRawScores GetPredationToolsRawScores(MicrobeSpecies microbeSpecies)
    {
        return predationScoring.GetPredationToolsRawScores(microbeSpecies);
    }

    public PredationToolsRawScores GetPredationToolsRawScores(MulticellularSpecies multicellularSpecies)
    {
        return predationScoring.GetPredationToolsRawScores(multicellularSpecies);
    }

    private float GetEnzymesScore(MicrobeSpecies predator, string dissolverEnzyme, float specializationBonus)
    {
        // This is not cached as it is not useful at the present time (as this is only called from places that cache
        // stuff)
        var organelles = predator.Organelles.Organelles;
        var isMembraneDigestible = dissolverEnzyme == Constants.LIPASE_ENZYME;
        var enzymesScore = 0.0f;

        if (isMembraneDigestible)
        {
            // Add the base digestion score that works even without any organelles added
            enzymesScore += Constants.AUTO_EVO_BASE_DIGESTION_SCORE;
        }

        var scoreInfo = Constants.AutoEvoLysosomeEnzymesScores;

        var count = organelles.Count;
        for (var i = 0; i < count; ++i)
        {
            var placedOrganelle = organelles[i];

            var enzyme = placedOrganelle.GetActiveTargetEnzyme(dissolverEnzyme);
            if (enzyme != null)
            {
                // No need to check the amount here as organelle data validates enzyme amounts are above 0

                isMembraneDigestible = true;

                // This doesn't use safety as it will be otherwise masking very subtle bugs with some enzyme not
                // working in auto-evo
                ref var individualScore =
                    ref CollectionsMarshal.GetValueRefOrNullRef(scoreInfo, enzyme.InternalName);
                if (Unsafe.IsNullRef(ref individualScore))
                    throw new InvalidOperationException("Missing enzyme score for: " + enzyme.InternalName);

                enzymesScore += individualScore;
            }
        }

        // If not digestible, mark that as a 0 score
        if (!isMembraneDigestible)
            return 0;

        return enzymesScore * specializationBonus;
    }

    private float GetEnzymesScore(IReadOnlyCellTypeDefinition cellType, string dissolverEnzyme,
        float specializationBonus)
    {
        // This is not cached as it is not useful at the present time (as this is only called from places that cache
        // stuff)
        var organelles = cellType.Organelles;
        var isMembraneDigestible = dissolverEnzyme == Constants.LIPASE_ENZYME;
        var enzymesScore = 0.0f;

        if (isMembraneDigestible)
        {
            // Add the base digestion score that works even without any organelles added
            enzymesScore += Constants.AUTO_EVO_BASE_DIGESTION_SCORE;
        }

        var scoreInfo = Constants.AutoEvoLysosomeEnzymesScores;

        foreach (var organelle in organelles)
        {
            var enzyme = organelle.GetActiveTargetEnzyme(dissolverEnzyme);
            if (enzyme != null)
            {
                // No need to check the amount here as organelle data validates enzyme amounts are above 0

                isMembraneDigestible = true;

                // This doesn't use safety as it will be otherwise masking very subtle bugs with some enzyme not
                // working in auto-evo
                ref var individualScore =
                    ref CollectionsMarshal.GetValueRefOrNullRef(scoreInfo, enzyme.InternalName);
                if (Unsafe.IsNullRef(ref individualScore))
                    throw new InvalidOperationException("Missing enzyme score for: " + enzyme.InternalName);

                enzymesScore += individualScore;
            }
        }

        // If not digestible, mark that as a 0 score
        if (!isMembraneDigestible)
            return 0;

        return enzymesScore * specializationBonus;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSpeciesCacheKey(Species species)
    {
        // Apparently, rather than a custom implementation of a hash combine, just using the one cache-method that
        // is present is better
        var knownCache = species.AutoEvoAttemptCache;

        if (knownCache != 0)
            return knownCache;

        // Assume everything that doesn't have a key set will be a long-lived object with a persistent hash code
        // return species.GetHashCode();
        // return RuntimeHelpers.GetHashCode(species);

        // Apparently the above is still quite bad for conflicts when not keeping the instances, so we need to do some
        // stuff here
        unchecked
        {
            var hash = RuntimeHelpers.GetHashCode(species);
            hash = HashCode.Combine(hash, species.Epithet.GetHashCode() * 6686041);

            if (hash < 40000 && hash >= 0)
                GD.Print("Got a low hash: " + hash + $"\t {species}");
            return hash;
        }

        // This is the variant that still causes cache conflicts
        /*unchecked
        {
            var rawHash = species.GetHashCode();
            rawHash *= 6686041;
            rawHash += species.AutoEvoAttemptCache;

            rawHash = int.RotateLeft(rawHash, 7);

            rawHash += species.ID.GetHashCode();
            rawHash *= 8144639;
            return rawHash;
        }*/
    }

#if CHECK_HASH_CODE_REUSED_INSTANCES
    private void CheckSpecies(MicrobeSpecies species)
    {
        var visual = species.GetVisualHashCode();

        var key = GetSpeciesCacheKey(species);

        if (!microbeHashCheckValues.TryGetValue(key, out var existing))
        {
#if CHECK_CACHE_STORE_INSTANCES
            microbeHashCheckValues[key] = species;
#else
            microbeHashCheckValues[key] = visual;
#endif
            return;
        }

        // Species has been modified, which is not optimal but technically not a fault of the cache
#if CHECK_CACHE_STORE_INSTANCES
        if (species == existing)
            return;
#endif

#if CHECK_CACHE_STORE_INSTANCES
        var visualHash = existing.GetVisualHashCode();
        var oldKey = GetSpeciesCacheKey(existing);

        if (visualHash != visual)
#else
        if (existing != visual)
#endif
        {
            GD.PrintErr($"Hash code reused for different species. Key: {key}, Visual: {visual}, Existing: {existing}");
        }

#if CHECK_CACHE_STORE_INSTANCES
        if (oldKey == key)
        {
            GD.PrintErr($"Hash code reused for different species. Key: {key}, Existing: {existing}");
        }

        microbeHashCheckValues[key] = species;
#else
        microbeHashCheckValues[key] = visual;
#endif
    }
#endif
}
