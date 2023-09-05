namespace UnlockConstraints
{
    using System.Linq;

    /// <summary>
    ///   A single constraint required for an organelle to be unlocked.
    ///   These constraints are collected in <see cref="OrganelleUnlockConstraints"/>.
    /// </summary>
    public interface IUnlockConstraint
    {
        string Display(GameWorld world, EnergyBalanceInfo energyBalance);
        bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance);
    }

    /// <summary>
    ///   The number of microbes engulfed by the player is above the specified amount.
    /// </summary>
    public struct EngulfedMicrobesAbove : IUnlockConstraint
    {
        public int Microbes;

        public readonly string Display(GameWorld world, EnergyBalanceInfo energyBalance) =>
            string.Format("Elgulfed {0}/{1} cells", world.TotalMicrobesEngulfedByPlayer, Microbes);

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance) =>
            world.TotalMicrobesEngulfedByPlayer >= Microbes;
    }

    /// <summary>
    ///   The number of times the player has died is above a specified amount.
    /// </summary>
    public struct PlayerDeathsAbove : IUnlockConstraint
    {
        public int Deaths;

        public readonly string Display(GameWorld world, EnergyBalanceInfo energyBalance) =>
            string.Format("Died {0}/{1} times", world.TotalPlayerDeaths, Deaths);

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance) => world.TotalPlayerDeaths >= Deaths;
    }

    /// <summary>
    ///   The player species produces more than a specified amount of APT.
    /// </summary>
    public struct AptAbove : IUnlockConstraint
    {
        public float Min;

        public readonly string Display(GameWorld world, EnergyBalanceInfo energyBalance) =>
            string.Format("ATP {0}/{1}", energyBalance.TotalProduction, Min);

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance) => energyBalance.TotalProduction >= Min;
    }

    /// <summary>
    ///   The player species produces a specified amount more APT than it uses.
    /// </summary>
    public struct ExcessAptAbove : IUnlockConstraint
    {
        public float Excess;

        public readonly string Display(GameWorld world, EnergyBalanceInfo energyBalance) =>
            string.Format("Excess ATP {0}/{1}", energyBalance.TotalProduction - energyBalance.TotalConsumptionStationary, Excess);

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance) =>
            energyBalance.TotalProduction - energyBalance.TotalConsumptionStationary >= Excess;
    }

    /// <summary>
    ///   The player species has a base speed that is slower than a certain speed.
    /// </summary>
    public struct BaseSpeedBelow : IUnlockConstraint
    {
        public float Speed;

        public readonly string Display(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            if (world.PlayerSpecies is not MicrobeSpecies microbeSpecies)
                return string.Empty;
            return string.Format("Speed below {0} (currently {1})", Speed, microbeSpecies.BaseSpeed);
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            if (world.PlayerSpecies is not MicrobeSpecies microbeSpecies)
                return false;
            return microbeSpecies.BaseSpeed <= Speed;
        }
    }

    /// <summary>
    ///   The player species has had a specific organelle for some number of generations.
    /// </summary>
    public struct ReproducedWith : IUnlockConstraint
    {
        // Cannot be of type `OrganelleDefenition` because it isn't populated until organelles are deserialised.
        public string Organelle { get; set; }
        public int Generations { get; set; }

        public readonly string Display(GameWorld world, EnergyBalanceInfo energyBalance) =>
            string.Format("{0} for {1}/{2} generations", SimulationParameters.Instance.GetOrganelleType(Organelle).Name, CountGenerations(world), Generations);

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance) =>
            CountGenerations(world) >= Generations;

        private readonly int CountGenerations(GameWorld world)
        {
            var playerSpecies = world.PlayerSpecies.ID;
            var totalGenerations = world.GenerationHistory.Count;
            var desiredOrganelle = SimulationParameters.Instance.GetOrganelleType(Organelle);

            for (var generation = totalGenerations - 1; generation > 0; generation--)
            {
                var species = world.GenerationHistory[generation].AllSpeciesData[playerSpecies].Species;

                if (species is not MicrobeSpecies microbeSpecies)
                    return totalGenerations - generation - 1;

                if (!microbeSpecies.Organelles.Any(organelle => organelle.Definition == desiredOrganelle))
                    return totalGenerations - generation - 1;
            }

            return totalGenerations;
        }
    }

    /// <summary>
    ///   The player has reproduced in a specific named biome.
    /// </summary>
    public struct ReproduceInBiome : IUnlockConstraint
    {
        public Biome Biome;

        public readonly string Display(GameWorld world, EnergyBalanceInfo energyBalance) =>
            string.Format("Reproduce in the {0} biome (currently in {1})", Biome.Name, world.Map.CurrentPatch!.BiomeTemplate);

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance) =>
            Biome == null || world.Map.CurrentPatch!.BiomeTemplate == Biome;
    }

    /// <summary>
    ///   The player's current biome has a certain amount of a compound.
    /// </summary>
    public struct BiomeCompound : IUnlockConstraint
    {
        public Compound Compound;
        public float? Min;
        public float? Max;

        public readonly string Display(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            var current = CompoundValue(world);
            if (Min.HasValue && Max.HasValue)
                return string.Format("{0} between {1} and {2} (current {3})", Compound.Name, Min, Max, current);
            if (Min.HasValue)
                return string.Format("{0} more than {1} (current {2})", Compound.Name, Min, current);
            if (Max.HasValue)
                return string.Format("{0} less than {1} (current {2})", Compound.Name, Max, current);
            return string.Empty;
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            var value = CompoundValue(world);
            var minSatisfied = !Min.HasValue || value >= Min;
            var maxSatisfied = !Max.HasValue || value <= Max;
            return minSatisfied && maxSatisfied;
        }

        private readonly float CompoundValue(GameWorld world) =>
            world.Map.CurrentPatch!.GetCompoundAmount(Compound);
    }
}
