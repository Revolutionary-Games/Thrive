namespace AutoEvo
{
    using System;
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
        private readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        private readonly Compound Mucilage = SimulationParameters.Instance.GetCompound("mucilage");
        private readonly Dictionary<(MicrobeSpecies, BiomeConditions), EnergyBalanceInfo> cachedEnergyBalances = new();
        private readonly Dictionary<MicrobeSpecies, float> cachedBaseSpeeds = new();
        private readonly Dictionary<MicrobeSpecies, float> cachedBaseHexSizes = new();
        private readonly Dictionary<MicrobeSpecies, float> cachedStorageCapacities = new();
        private readonly Dictionary<(MicrobeSpecies, BiomeConditions, Compound), float> cachedCompoundScores = new();
        private readonly Dictionary<MicrobeSpecies, MicrobeOrganelleData> cachedOrganelleData = new();

        private readonly Dictionary<(TweakedProcess, BiomeConditions), ProcessSpeedInformation> cachedProcessSpeeds =
            new();

        public SimulationCache(WorldGenerationSettings worldSettings)
        {
            this.worldSettings = worldSettings;
        }

        public EnergyBalanceInfo GetEnergyBalanceForSpecies(MicrobeSpecies species, BiomeConditions biomeConditions)
        {
            var key = (species, biomeConditions);

            if (cachedEnergyBalances.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // Auto-evo uses the average values of compound during the course of a simulated day
            cached = ProcessSystem.ComputeEnergyBalance(species.Organelles, biomeConditions, species.MembraneType,
                species.PlayerSpecies, worldSettings, CompoundAmountType.Average);

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

        public float GetCompoundUseScoreForSpecies(MicrobeSpecies species, BiomeConditions biomeConditions,
            Compound compound)
        {
            var key = (species, biomeConditions, compound);

            if (cachedCompoundScores.TryGetValue(key, out var cached))
            {
                return cached;
            }

            cached = 0.0f;

            // We check generation from all the processes of the cell../
            foreach (var organelle in species.Organelles)
            {
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    // ... that uses the given compound (regardless of usage)
                    if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                    {
                        var processEfficiency = GetProcessMaximumSpeed(process, biomeConditions).Efficiency;

                        cached += inputAmount * processEfficiency;
                    }
                }
            }

            cachedCompoundScores.Add(key, cached);
            return cached;
        }

        /// <summary>
        ///   Calculates a maximum speed for a process that can happen given the environmental. Environmental compounds
        ///   are always used at the average amount in auto-evo.
        /// </summary>
        /// <param name="process">The process to calculate the speed for</param>
        /// <param name="biomeConditions">The biome conditions to use</param>
        /// <returns>The speed information for the process</returns>
        public ProcessSpeedInformation GetProcessMaximumSpeed(TweakedProcess process, BiomeConditions biomeConditions)
        {
            var key = (process, biomeConditions);

            if (cachedProcessSpeeds.TryGetValue(key, out var cached))
            {
                return cached;
            }

            cached = ProcessSystem.CalculateProcessMaximumSpeed(process, biomeConditions, CompoundAmountType.Average);

            cachedProcessSpeeds.Add(key, cached);
            return cached;
        }

        public bool MatchesSettings(WorldGenerationSettings checkAgainst)
        {
            return worldSettings.Equals(checkAgainst);
        }

        public MicrobeOrganelleData GetOrganelleData(MicrobeSpecies species)
        {
            if (cachedOrganelleData.TryGetValue(species, out var cached))
            {
                return cached;
            }

            int pilusCount = 0;
            float oxytoxyCount = 0.0f;
            float mucilageCount = 0.0f;
            foreach (var organelle in species.Organelles)
            {
                if (organelle.Definition.HasPilusComponent)
                {
                    pilusCount += 1;
                    continue;
                }

                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Outputs.TryGetValue(Oxytoxy, out var oxytoxyAmount))
                    {
                        oxytoxyCount += oxytoxyAmount;
                    }

                    if (process.Process.Outputs.TryGetValue(Mucilage, out var mucilageAmount))
                    {
                        mucilageCount += mucilageAmount;
                    }
                }
            }

            cached = new MicrobeOrganelleData(pilusCount, oxytoxyCount, mucilageCount);

            cachedOrganelleData.Add(species, cached);

            return cached;
        }

        public class MicrobeOrganelleData
        {
            public int PilusCount = 0;
            public float OxytoxyCount = 0.0f;
            public float MucilageCount = 0.0f;

            public MicrobeOrganelleData(int pilusCount, float oxytoxyCount, float mucilageCount)
            {
                PilusCount = pilusCount;
                OxytoxyCount = oxytoxyCount;
                MucilageCount = mucilageCount;
            }
        }
    }
}
