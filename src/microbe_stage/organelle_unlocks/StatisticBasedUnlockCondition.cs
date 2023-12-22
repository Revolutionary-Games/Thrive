namespace UnlockConstraints
{
    using System.Collections.Generic;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   An unlock condition that relies on a certain <see cref="GenericStatistic{T}"/> to track progress
    /// </summary>
    public abstract class StatisticBasedUnlockCondition : IUnlockCondition
    {
        [JsonIgnore]
        public abstract StatsTrackerEvent RelevantEvent { get; }

        [JsonIgnore]
        public IStatistic? RelevantStatistic { get; set; }

        public virtual void OnInit()
        {
            return;
        }

        public abstract bool Satisfied();

        public abstract void GenerateTooltip(LocalizedStringBuilder builder);
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

        public string Biome { get; set; } = string.Empty;

        [JsonIgnore]
        public ReproductionStatistic? ReproductionStatistc => (ReproductionStatistic?)RelevantStatistic;

        public override void OnInit()
        {
            if (string.IsNullOrEmpty(Biome))
                throw new InvalidRegistryDataException("ReproduceInBiome unlock condition Biome field is empty");

            try
            {
                biome = SimulationParameters.Instance.GetBiome(Biome);
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidRegistryDataException("ReproduceInBiome unlock condition has invalid biome");
            }
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (ReproductionStatistc == null)
                return;

            builder.Append(new LocalizedString("REPRODUCE_IN_BIOME", biome));
        }

        public override bool Satisfied()
        {
            if (ReproductionStatistc == null)
                return false;

            if (!ReproductionStatistc.ReproducedInBiomes.TryGetValue(Biome, out var count))
                return false;

            return count >= 1;
        }
    }

    public class ReproduceWithOrganelle : StatisticBasedUnlockCondition
    {
        [JsonIgnore]
        private OrganelleDefinition organelle = null!;

        public override StatsTrackerEvent RelevantEvent => StatsTrackerEvent.PlayerReproduced;

        public string Organelle { get; set; } = string.Empty;

        [JsonProperty]
        public int Generations { get; set; } = 1;

        [JsonIgnore]
        public ReproductionStatistic? ReproductionStatistic => (ReproductionStatistic?)RelevantStatistic;

        public override void OnInit()
        {
            if (string.IsNullOrEmpty(Organelle))
                throw new InvalidRegistryDataException("ReproduceWithOrganelle unlock condition Organelle field is empty");

            try
            {
                organelle = SimulationParameters.Instance.GetOrganelleType(Organelle);
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidRegistryDataException("ReproduceWithOrganelle unlock condition has invalid organelle");
            }
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (ReproductionStatistic == null)
                return;

            if (!ReproductionStatistic.ReproducedWithOrganelle.TryGetValue(Organelle, out var count))
                count = 0;

            builder.Append(new LocalizedString("REPRODUCED_WITH", organelle, Generations, count));
        }

        public override bool Satisfied()
        {
            if (ReproductionStatistic == null)
                return false;

            if (!ReproductionStatistic.ReproducedWithOrganelle.TryGetValue(Organelle, out var count))
                return false;

            return count >= Generations;
        }
    }
}
