namespace UnlockConstraints
{
    using Newtonsoft.Json;

    /// <summary>
    ///   An unlock condition that relies on a certain <see cref="IStatistic"/> to track progress
    /// </summary>
    public abstract class StatisticBasedUnlockCondition : IUnlockCondition
    {
        [JsonIgnore]
        public abstract StatsTrackerEvent RelevantEvent { get; }

        [JsonIgnore]
        public IStatistic? RelevantStatistic { get; set; }

        public abstract bool Satisfied();

        public abstract void GenerateTooltip(LocalizedStringBuilder builder);

        public virtual void Check(string name)
        {
        }

        public virtual void Resolve(SimulationParameters parameters)
        {
        }
    }

    /// <summary>
    ///   The number of microbes engulfed by the player is above the specified amount.
    /// </summary>
    public class EngulfedMicrobesAbove : StatisticBasedUnlockCondition
    {
        public override StatsTrackerEvent RelevantEvent => StatsTrackerEvent.PlayerEngulfedOther;

        [JsonProperty]
        public int Required { get; set; }

        [JsonIgnore]
        public SimpleStatistic? Engulfed => (SimpleStatistic?)RelevantStatistic;

        public override bool Satisfied()
        {
            if (Engulfed == null)
                return false;

            return Engulfed.Value >= Required;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (Engulfed == null)
                return;

            builder.Append(new LocalizedString("ENGULFED_MICROBES_ABOVE", Required, Engulfed.Value));
        }
    }

    /// <summary>
    ///   The number of times the player has died is above a specified amount.
    /// </summary>
    public class PlayerDeathsAbove : StatisticBasedUnlockCondition
    {
        public override StatsTrackerEvent RelevantEvent => StatsTrackerEvent.PlayerDied;

        [JsonProperty]
        public int Required { get; set; }

        [JsonIgnore]
        public SimpleStatistic? Deaths => (SimpleStatistic?)RelevantStatistic;

        public override bool Satisfied()
        {
            if (Deaths == null)
                return false;

            return Deaths.Value >= Required;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (Deaths == null)
                return;

            builder.Append(new LocalizedString("PLAYER_DEATH_COUNT_ABOVE", Required, Deaths.Value));
        }
    }

    public class ReproduceInBiome : StatisticBasedUnlockCondition
    {
        [JsonIgnore]
        private Biome biome = null!;

        public override StatsTrackerEvent RelevantEvent => StatsTrackerEvent.PlayerReproduced;

        [JsonProperty("Biome")]
        public string RawBiome { get; set; } = string.Empty;

        [JsonIgnore]
        public ReproductionStatistic? ReproductionStatistc => (ReproductionStatistic?)RelevantStatistic;

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (ReproductionStatistc == null)
                return;

            builder.Append(new LocalizedString("REPRODUCE_IN_BIOME", biome.Name));
        }

        public override bool Satisfied()
        {
            if (ReproductionStatistc == null)
                return false;

            if (!ReproductionStatistc.ReproducedInBiomes.TryGetValue(RawBiome, out var count))
                return false;

            return count >= 1;
        }

        public override void Check(string name)
        {
            if (string.IsNullOrEmpty(RawBiome))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Biome is empty");
            }
        }

        public override void Resolve(SimulationParameters parameters)
        {
            biome = parameters.GetBiome(RawBiome);
        }
    }

    public class ReproduceWithOrganelle : StatisticBasedUnlockCondition
    {
        [JsonIgnore]
        private OrganelleDefinition organelle = null!;

        public override StatsTrackerEvent RelevantEvent => StatsTrackerEvent.PlayerReproduced;

        [JsonProperty("Organelle")]
        public string RawOrganelle { get; set; } = string.Empty;

        [JsonProperty]
        public int Generations { get; set; } = 1;

        [JsonIgnore]
        public ReproductionStatistic? ReproductionStatistic => (ReproductionStatistic?)RelevantStatistic;

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (ReproductionStatistic == null)
                return;

            if (!ReproductionStatistic.ReproducedWithOrganelle.TryGetValue(RawOrganelle, out var count))
                count = 0;

            builder.Append(new LocalizedString("REPRODUCED_WITH", organelle, Generations, count));
        }

        public override bool Satisfied()
        {
            if (ReproductionStatistic == null)
                return false;

            if (!ReproductionStatistic.ReproducedWithOrganelle.TryGetValue(RawOrganelle, out var count))
                return false;

            return count >= Generations;
        }

        public override void Check(string name)
        {
            if (string.IsNullOrEmpty(RawOrganelle))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Organelle is empty");
            }
        }

        public override void Resolve(SimulationParameters parameters)
        {
            organelle = parameters.GetOrganelleType(RawOrganelle);
        }
    }
}
