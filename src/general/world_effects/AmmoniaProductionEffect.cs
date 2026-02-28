using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using SharedBase.Archive;
using Systems;

/// <summary>
///   Creates ammonia based on species with nitrogen fixing organelles. And consumes ammonia based on the ammonia
///   reproduction cost of species.
/// </summary>
public class AmmoniaProductionEffect : IWorldEffect
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly List<TweakedProcess> microbeProcesses = new();

    private readonly GameWorld targetWorld;

    public AmmoniaProductionEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.AmmoniaProductionEffect;

    public bool CanBeReferencedInArchive => false;

    public static AmmoniaProductionEffect ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new AmmoniaProductionEffect(reader.ReadObject<GameWorld>());
    }

    public static float GetSpeciesModifiersForEffect(Species species, out ResolvedMicrobeTolerances resolvedTolerances,
        out float specialization, List<TweakedProcess> microbeProcesses, IBiomeConditions biome,
        WorldGenerationSettings worldGenerationSettings)
    {
        resolvedTolerances = new ResolvedMicrobeTolerances
        {
            ProcessSpeedModifier = 1,
            OsmoregulationModifier = 1,
            HealthModifier = 1,
        };

        specialization = 1;

        if (species is MicrobeSpecies microbeSpecies)
        {
            resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
                MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(microbeSpecies, biome));

            specialization = microbeSpecies.SpecializationBonus;
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
                MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(multicellularSpecies, biome));

            specialization = multicellularSpecies.CalculateAverageSpecialization();
        }

        if (specialization <= 0)
        {
            GD.PrintErr("Uninitialized species specialization in a world effect");
        }

        // TODO: this also doesn't work for multicellular species!
        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/6758
        return ProcessSystem.CalculateSpeciesActiveProcessListForEffect(species,
            microbeProcesses, biome, resolvedTolerances, specialization, worldGenerationSettings);
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(targetWorld);
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        ApplyCompoundsAddition();
    }

    private void ApplyCompoundsAddition()
    {
        microbeProcesses.Clear();

        foreach (var patchKeyValue in targetWorld.Map.Patches)
        {
            var patch = patchKeyValue.Value;

            // Skip empty patches as they don't need handling
            if (patch.SpeciesInPatch.Count < 1)
                continue;

            var ammonia = patch.Biome.ChangeableCompounds[Compound.Ammonia];

            float ammoniaBalance = 0;

            foreach (var species in patch.SpeciesInPatch)
            {
                var balanceModifier = GetSpeciesModifiersForEffect(species.Key, out var resolvedTolerances,
                    out var specialization, microbeProcesses, patch.Biome, targetWorld.WorldSettings);

                // Ammonia consumption of this species based on reproduction cost of all individuals
                ammoniaBalance -= (float)(Constants.AMMONIA_ENVIRONMENT_CONSUMPTION_MULTIPLIER * species.Value *
                    species.Key.TotalReproductionCost[Compound.Ammonia]);

                // Process-related production of Ammonia
                foreach (var process in microbeProcesses)
                {
                    // Only handle relevant processes
                    if (!IsProcessRelevant(process.Process))
                        continue;

                    var effectiveSpeed =
                        ProcessSystem.CalculateEffectiveProcessSpeedForEffect(process, balanceModifier, patch.Biome,
                            resolvedTolerances.ProcessSpeedModifier, specialization);

                    if (effectiveSpeed <= 0)
                        continue;

                    // Currently, no processes have Ammonia as input

                    // Outputs generate the given compounds
                    foreach (var output in process.Process.Outputs)
                    {
                        if (output.Key.ID is Compound.Ammonia)
                        {
                            // Calculated as a double as the multipliers are really low to get this into a reasonable
                            // range
                            ammoniaBalance += (float)(output.Value *
                                Constants.AMMONIA_ENVIRONMENT_PRODUCTION_MULTIPLIER * effectiveSpeed *
                                species.Value);
                        }
                    }
                }
            }

            // Scale the balances to make the changes less drastic
            if (ammoniaBalance == 0)
                continue;

            ammoniaBalance *= Constants.AMMONIA_ENVIRONMENT_SPEED_MULTIPLIER;

            ammonia.Density += ammoniaBalance;

            patch.Biome.ModifyLongTermCondition(Compound.Ammonia, ammonia);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsProcessRelevant(BioProcess process)
    {
        foreach (var output in process.Outputs)
        {
            if (output.Key.ID is Compound.Ammonia)
                return true;
        }

        return false;
    }
}
