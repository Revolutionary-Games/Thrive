namespace AutoEvo
{
    using System.Collections.Generic;

    /// <summary>
    ///   Caches some information in auto-evo runs to speed them up
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Some information will get outdated when data that the auto-evo relies on changes. If in the future
    ///     caching is moved to a higher level in the auto-evo, that needs to be considered.
    ///   </para>
    /// </remarks>
    public class SimulationCache
    {
        private readonly WorldGenerationSettings worldSettings;
        private readonly Dictionary<(MicrobeSpecies, BiomeConditions), EnergyBalanceInfo> cachedEnergyBalances = new();
        private readonly Dictionary<MicrobeSpecies, float> cachedBaseSpeeds = new();
        private readonly Dictionary<MicrobeSpecies, float> cachedBaseHexSizes = new();
        private readonly Dictionary<MicrobeSpecies, float> cachedStorageCapacities = new();

        private readonly Dictionary<(TweakedProcess, BiomeConditions), ProcessSpeedInformation> cachedProcessSpeeds =
            new();

        public SimulationCache(WorldGenerationSettings worldSettings)
        {
            this.worldSettings = worldSettings;
        }

        public bool DayNightCycleEnabled => !worldSettings.DayNightEnabled;

        public EnergyBalanceInfo GetEnergyBalanceForSpecies(MicrobeSpecies species, BiomeConditions biomeConditions)
        {
            var key = (species, biomeConditions);

            if (cachedEnergyBalances.TryGetValue(key, out var cached))
            {
                return cached;
            }

            cached = ProcessSystem.ComputeEnergyBalance(species.Organelles, biomeConditions, species.MembraneType,
                species.PlayerSpecies, worldSettings);

            cachedEnergyBalances.Add(key, cached);
            return cached;
        }

        public float GetBaseSpeedForSpecies(MicrobeSpecies species)
        {
            if (cachedBaseSpeeds.TryGetValue(species, out var cached))
            {
                return cached;
            }

            cached = species.BaseSpeed;

            cachedBaseSpeeds.Add(species, cached);
            return cached;
        }

        public float GetBaseHexSizeForSpecies(MicrobeSpecies species)
        {
            if (cachedBaseHexSizes.TryGetValue(species, out var cached))
            {
                return cached;
            }

            cached = species.BaseHexSize;

            cachedBaseHexSizes.Add(species, cached);
            return cached;
        }

        public float GetStorageCapacityForSpecies(MicrobeSpecies species)
        {
            if (cachedStorageCapacities.TryGetValue(species, out var cached))
                return cached;

            cached = species.StorageCapacity;

            cachedStorageCapacities.Add(species, cached);
            return cached;
        }

        public ProcessSpeedInformation GetProcessMaximumSpeed(TweakedProcess process, BiomeConditions biomeConditions)
        {
            var key = (process, biomeConditions);

            if (cachedProcessSpeeds.TryGetValue(key, out var cached))
            {
                return cached;
            }

            cached = ProcessSystem.CalculateProcessMaximumSpeed(process, biomeConditions);

            cachedProcessSpeeds.Add(key, cached);
            return cached;
        }

        public bool MatchesSettings(WorldGenerationSettings checkAgainst)
        {
            return worldSettings.Equals(checkAgainst);
        }
    }
}
