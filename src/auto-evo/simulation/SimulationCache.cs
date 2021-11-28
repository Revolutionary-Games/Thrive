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
        private readonly Dictionary<(MicrobeSpecies, Patch), EnergyBalanceInfo> cachedEnergyBalances = new();

        public EnergyBalanceInfo GetEnergyBalanceForSpecies(MicrobeSpecies species, Patch patch)
        {
            var key = (species, patch);

            if (cachedEnergyBalances.TryGetValue(key, out var cached))
            {
                return cached;
            }

            cached = ProcessSystem.ComputeEnergyBalance(species.Organelles, patch.Biome, species.MembraneType);

            cachedEnergyBalances.Add(key, cached);
            return cached;
        }
    }
}
