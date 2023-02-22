namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    public class ChunkFoodSource : RandomEncounterFoodSource
    {
        private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
        private readonly Compound iron = SimulationParameters.Instance.GetCompound("iron");

        private readonly Patch patch;
        private readonly float totalEnergy;
        private readonly float chunkSize;
        private readonly string? chunkName;
        private readonly Dictionary<Compound, float>? energyCompounds;

        public ChunkFoodSource(Patch patch, string chunkType)
        {
            this.patch = patch;

            if (patch.Biome.Chunks.TryGetValue(chunkType, out ChunkConfiguration chunk) && chunk.Compounds != null)
            {
                chunkSize = chunk.Size;
                chunkName = chunk.Name;

                // NOTE: Here we use the heuristic that only iron and glucose are useful in chunks
                energyCompounds = chunk.Compounds.Where(c => c.Key == iron || c.Key == glucose).ToDictionary(
                    c => c.Key, c => c.Value.Amount);

                // This computation nerfs big chunks with a large amount,
                // by adding an "accessibility" component to total energy.
                // Since most cells will rely on bigger chunks by exploiting the venting,
                // this technically makes it a less efficient food source than small chunks, despite a larger amount.
                // We thus account for venting also in the total energy from the source,
                // by adding a volume-to-surface radius exponent ratio (e.g. 2/3 for a sphere).
                // This logic doesn't match with the rest of auto-evo (which doesn't account for accessibility).
                // TODO: extend this approach or find another nerf.
                var ventedEnergy = Mathf.Pow(energyCompounds.Sum(c => c.Value), Constants.AUTO_EVO_CHUNK_AMOUNT_NERF);
                totalEnergy = ventedEnergy * chunk.Density * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
            }
        }

        public override float FitnessScore(Species species, SimulationCache simulationCache,
            WorldGenerationSettings worldSettings)
        {
            if (energyCompounds == null)
                throw new InvalidOperationException("Food source not valid for this patch");

            var microbeSpecies = (MicrobeSpecies)species;

            var energyBalance = simulationCache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

            // Don't penalize species that can't move at full speed all the time as much here
            var baseSpeed = simulationCache.GetBaseSpeedForSpecies(microbeSpecies);
            var chunkEaterSpeed = Math.Max(baseSpeed + energyBalance.FinalBalance,
                baseSpeed / 3);

            // We ponder the score for each compound by its amount, leading to pondering in proportion of total
            // quantity, with a constant factor that will be eliminated when making ratios of scores for this niche.
            var score = energyCompounds.Sum(c => CompoundUseScore(
                microbeSpecies, c.Key, patch, simulationCache, worldSettings) * c.Value);

            score *= chunkEaterSpeed * species.Behaviour.Activity;

            // If the species can't engulf, then they are dependent on only eating the runoff compounds
            if (!microbeSpecies.CanEngulf ||
                simulationCache.GetBaseHexSizeForSpecies(microbeSpecies) < chunkSize * Constants.ENGULF_SIZE_RATIO_REQ)
            {
                score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
            }

            // Chunk (originally from marine snow) food source penalizes big creatures that try to rely on it
            score /= energyBalance.TotalConsumptionStationary;

            return score;
        }

        public override IFormattable GetDescription()
        {
            return new LocalizedString("CHUNK_FOOD_SOURCE",
                string.IsNullOrEmpty(chunkName) ?
                    new LocalizedString("NOT_FOUND_CHUNK") :
                    new LocalizedString(chunkName!));
        }

        public override float TotalEnergyAvailable()
        {
            return totalEnergy;
        }
    }
}
