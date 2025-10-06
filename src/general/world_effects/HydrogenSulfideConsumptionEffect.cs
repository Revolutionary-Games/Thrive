using System.Collections.Generic;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Reduces hydrogen sulfide based on how many cells are eating it. This is needed to balance out
///   <see cref="UnderwaterVentEruptionEffect"/> otherwise adding infinite hydrogen sulfide. This has a minimum floor
///   to ensure that if eruptions don't happen enough that hydrogen sulfide eaters aren't completely nonviable.
/// </summary>
[JSONDynamicTypeAllowed]
public class HydrogenSulfideConsumptionEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    public HydrogenSulfideConsumptionEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        List<TweakedProcess> microbeProcesses = [];

        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            // Skip patches that don't need handling
            if (!patch.Biome.ChangeableCompounds.TryGetValue(Compound.Hydrogensulfide,
                    out BiomeCompoundProperties hydrogenSulfide) || hydrogenSulfide.Density <= 0)
            {
                continue;
            }

            // Reduce the amount if there are species consuming it
            bool speciesEatIt = false;
            foreach (var species in patch.SpeciesInPatch)
            {
                var resolvedTolerances = new ResolvedMicrobeTolerances
                {
                    ProcessSpeedModifier = 1,
                    OsmoregulationModifier = 1,
                    HealthModifier = 1,
                };

                // TODO: multicellular environmental tolerances
                if (species.Key is MicrobeSpecies microbeSpecies)
                {
                    resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(
                        MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(microbeSpecies, patch.Biome));
                }

                var balanceModifier = ProcessSystem.CalculateSpeciesActiveProcessListForEffect(species.Key,
                    microbeProcesses, patch.Biome, resolvedTolerances, targetWorld.WorldSettings);

                foreach (var process in microbeProcesses)
                {
                    // Only handle relevant processes
                    if (!IsProcessRelevant(process.Process))
                        continue;

                    var effectiveSpeed =
                        ProcessSystem.CalculateEffectiveProcessSpeedForEffect(process, balanceModifier, patch.Biome,
                            resolvedTolerances.ProcessSpeedModifier);

                    if (effectiveSpeed <= 0)
                        continue;

                    // Inputs take away compounds scaled by the speed and population of the species
                    foreach (var input in process.Process.Inputs)
                    {
                        if (input.Key.ID is Compound.Hydrogensulfide)
                        {
                            // Calculated as a double as the multipliers are really low to get this into a reasonable
                            // range
                            hydrogenSulfide.Density -= (float)(input.Value *
                                Constants.HYDROGEN_SULFIDE_ENVIRONMENT_EATING_MULTIPLIER * effectiveSpeed *
                                species.Value);
                            speciesEatIt = true;
                        }
                    }

                    // For now there are no processes that produce hydrogen sulfide, but if some are added in the
                    // future, then some kind of max is probably needed to be configured
                }
            }

            var minimum =
                patch.BiomeTemplate.Conditions.GetCompound(Compound.Hydrogensulfide, CompoundAmountType.Biome);

            if (minimum.Density <= MathUtils.EPSILON)
            {
                if (!speciesEatIt)
                {
                    // In patches where there is no hydrogen sulfide naturally (it must have diffused there from
                    // elsewhere) have some natural decay to make sure the whole world isn't full of it
                    hydrogenSulfide.Density -=
                        hydrogenSulfide.Density * Constants.HYDROGEN_SULFIDE_NATURAL_DECAY_FACTOR;
                }

                // When oxygen is present, the natural decay is increased (and present even if species eat it)
                // to make the world once oxygenated much less full of hydrogen sulfide
                var oxygen = patch.Biome.GetCompound(Compound.Oxygen, CompoundAmountType.Biome);

                var oxygenSteps = oxygen.Ambient / Constants.HYDROGEN_SULFIDE_NATURAL_DECAY_INCREASE_PER_OXYGEN;
                if (oxygenSteps > 0)
                {
                    hydrogenSulfide.Density -= hydrogenSulfide.Density *
                        Constants.HYDROGEN_SULFIDE_NATURAL_DECAY_FACTOR_OXYGEN * oxygenSteps;

                    // Higher min cutoff when there is some oxygen present
                    if (hydrogenSulfide.Density < Constants.HYDROGEN_SULFIDE_OXYGEN_TOTAL_CUTOFF)
                        hydrogenSulfide.Density = 0;
                }

                if (hydrogenSulfide.Density < MathUtils.EPSILON * 100)
                    hydrogenSulfide.Density = 0;
            }

            // If fell below the minimum, cannot change
            if (hydrogenSulfide.Density < minimum.Density * Constants.MIN_HYDROGEN_SULFIDE_FRACTION)
                hydrogenSulfide.Density = minimum.Density * Constants.MIN_HYDROGEN_SULFIDE_FRACTION;

            patch.Biome.ModifyLongTermCondition(Compound.Hydrogensulfide, hydrogenSulfide);
        }
    }

    private bool IsProcessRelevant(BioProcess process)
    {
        foreach (var input in process.Inputs)
        {
            if (input.Key.ID is Compound.Hydrogensulfide)
                return true;
        }

        return false;
    }
}
