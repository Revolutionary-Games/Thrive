namespace UnlockConstraints
{
    using System.Linq;

    /// <summary>
    ///   A single constraint required for an organelle to be unlocked.
    ///   These constraints are collected in <see cref="OrganelleUnlockConstraints"/>.
    /// </summary>
    public interface IUnlockConstraint
    {
        public void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value);
        public bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance);
    }

    /// <summary>
    ///   The number of microbes engulfed by the player is above the specified amount.
    /// </summary>
    public struct EngulfedMicrobesAbove : IUnlockConstraint
    {
        public int Microbes;

        public readonly void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value)
        {
            var currentlyEngulfed = world.TotalMicrobesEngulfedByPlayer;
            value.Append(new LocalizedString("ENGULFED_MICROBES_ABOVE", Microbes, currentlyEngulfed));
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            return world.TotalMicrobesEngulfedByPlayer >= Microbes;
        }
    }

    /// <summary>
    ///   The number of times the player has died is above a specified amount.
    /// </summary>
    public struct PlayerDeathsAbove : IUnlockConstraint
    {
        public int Deaths;

        public readonly void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value)
        {
            value.Append(new LocalizedString("PLAYER_DEATH_COUNT_ABOVE", Deaths, world.TotalPlayerDeaths));
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            return world.TotalPlayerDeaths >= Deaths;
        }
    }

    /// <summary>
    ///   The player species produces more than a specified amount of APT.
    /// </summary>
    public struct AptAbove : IUnlockConstraint
    {
        public float Atp;

        public readonly void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value)
        {
           value.Append(new LocalizedString("APT_ABOVE", Atp, energyBalance.TotalProduction));
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            return energyBalance.TotalProduction >= Atp;
        }
    }

    /// <summary>
    ///   The player species produces a specified amount more APT than it uses.
    /// </summary>
    public struct ExcessAptAbove : IUnlockConstraint
    {
        public float Excess;

        public readonly void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value)
        {
            var currentExcess = energyBalance.TotalProduction - energyBalance.TotalConsumptionStationary;
            value.Append(new LocalizedString("EXCESS_APT_ABOVE", Excess, currentExcess));
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            return energyBalance.TotalProduction - energyBalance.TotalConsumptionStationary >= Excess;
        }
    }

    /// <summary>
    ///   The player species has a base speed that is slower than a certain speed.
    /// </summary>
    public struct BaseSpeedBelow : IUnlockConstraint
    {
        public float Speed;

        public readonly void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value)
        {
            if (world.PlayerSpecies is not MicrobeSpecies microbeSpecies)
                return;

            value.Append(new LocalizedString("BASE_SPEED_BELOW", Speed, microbeSpecies.BaseSpeed));
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

        public readonly void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value)
        {
            var organelle = SimulationParameters.Instance.GetOrganelleType(Organelle).Name;
            value.Append(new LocalizedString("REPRODUCED_WITH", organelle, Generations, CountGenerations(world)));
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            return CountGenerations(world) >= Generations;
        }

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

        public readonly void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value)
        {
            value.Append(new LocalizedString("REPRODUCE_IN_BIOME", Biome.Name, world.Map.CurrentPatch!.BiomeTemplate));
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            return Biome == null || world.Map.CurrentPatch!.BiomeTemplate == Biome;
        }
    }

    /// <summary>
    ///   The player's current biome has a certain amount of a compound.
    /// </summary>
    public struct BiomeCompound : IUnlockConstraint
    {
        public Compound Compound;
        public float? Min;
        public float? Max;

        public readonly void Tooltip(GameWorld world, EnergyBalanceInfo energyBalance, LocalizedStringBuilder value)
        {
            var current = CompoundValue(world);
            var compound = Compound.InternalName;
            if (Min.HasValue && Max.HasValue)
                value.Append(new LocalizedString("COMPOUND_IS_BETWEEN", compound, Min, Max, current));
            else if (Min.HasValue)
                value.Append(new LocalizedString("COMPOUND_IS_ABOVE", compound, Min, current));
            else if (Max.HasValue)
                value.Append(new LocalizedString("COMPOUND_IS_BELOW", compound, Max, current));
        }

        public readonly bool Satisfied(GameWorld world, EnergyBalanceInfo energyBalance)
        {
            var value = CompoundValue(world);
            var minSatisfied = !Min.HasValue || value >= Min;
            var maxSatisfied = !Max.HasValue || value <= Max;
            return minSatisfied && maxSatisfied;
        }

        private readonly float CompoundValue(GameWorld world)
        {
            return world.Map.CurrentPatch!.GetCompoundAmount(Compound);
        }
    }
}
