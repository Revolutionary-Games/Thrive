namespace UnlockConstraints
{
    using System;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    ///   An unlock condition that relies on a certain <see cref="IStatistic"/> to track progress
    /// </summary>
    public abstract class StatisticBasedUnlockCondition : IUnlockCondition
    {
        public abstract bool Satisfied(IUnlockStateDataSource data);

        public abstract void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data);

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
        [JsonProperty]
        public int Required { get; set; }

        public override bool Satisfied(IUnlockStateDataSource data)
        {
            if (data is not WorldStatsTracker tracker)
                return false;

            var engulfed = tracker.TotalEngulfedByPlayer;

            return engulfed.Value >= Required;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
        {
            if (data is not WorldStatsTracker tracker)
                return;

            var engulfed = tracker.TotalEngulfedByPlayer;

            builder.Append(new LocalizedString("UNLOCK_CONDITION_ENGULFED_MICROBES_ABOVE", Required,
                engulfed.Value));
        }
    }

    public class DigestedMicrobesAbove : StatisticBasedUnlockCondition
    {
        [JsonProperty]
        public int Required { get; set; }

        public override bool Satisfied(IUnlockStateDataSource data)
        {
            if (data is not WorldStatsTracker tracker)
                return false;

            var digested = tracker.TotalDigestedByPlayer;

            return digested.Value >= Required;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
        {
            if (data is not WorldStatsTracker tracker)
                return;

            var digested = tracker.TotalDigestedByPlayer;

            builder.Append(new LocalizedString("UNLOCK_CONDITION_DIGESTED_MICROBES_ABOVE", Required,
                digested.Value));
        }
    }

    /// <summary>
    ///   The number of times the player has died is above a specified amount.
    /// </summary>
    public class PlayerDeathsAbove : StatisticBasedUnlockCondition
    {
        [JsonProperty]
        public int Required { get; set; }

        public override bool Satisfied(IUnlockStateDataSource data)
        {
            if (data is not WorldStatsTracker tracker)
                return false;

            var deaths = tracker.TotalPlayerDeaths;

            return deaths.Value >= Required;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
        {
            if (data is not WorldStatsTracker tracker)
                return;

            var deaths = tracker.TotalPlayerDeaths;

            builder.Append(new LocalizedString("UNLOCK_CONDITION_PLAYER_DEATH_COUNT_ABOVE", Required,
                deaths.Value));
        }
    }

    /// <summary>
    ///   The player has reproduced in a certain biome
    /// </summary>
    public class ReproduceInBiome : StatisticBasedUnlockCondition
    {
        [JsonProperty]
        public Biome? Biome { get; set; }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
        {
            builder.Append(new LocalizedString("UNLOCK_CONDITION_REPRODUCE_IN_BIOME", Biome!.Name));
        }

        public override bool Satisfied(IUnlockStateDataSource data)
        {
            if (data is not WorldStatsTracker tracker)
                return false;

            var reproductionStat = tracker.PlayerReproductionStatistic;

            if (!reproductionStat.ReproducedInBiomes.TryGetValue(Biome!, out var count))
                return false;

            return count >= 1;
        }

        public override void Check(string name)
        {
            if (Biome == null)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Biome is null");
            }
        }
    }

    /// <summary>
    ///   The player has evolved a certain number of times with a certain amount of an organelle
    /// </summary>
    public class ReproduceWithOrganelle : StatisticBasedUnlockCondition
    {
        [JsonIgnore]
        private OrganelleDefinition organelle = null!;

        /// <summary>
        ///   The name of the organelle. This needs to be a string as having it be a definition would
        ///   cause a circular dependency, as unlock conditions are defined in organelles.
        /// </summary>
        [JsonProperty("Organelle")]
        public string RawOrganelle { get; set; } = string.Empty;

        [JsonProperty]
        public int Generations { get; set; } = 1;

        [JsonProperty]
        public int MinimumCount { get; set; } = 1;

        [JsonProperty]
        public bool InARow { get; set; }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data1)
        {
            if (data1 is not WorldStatsTracker tracker)
                return;

            var reproductionStat = tracker.PlayerReproductionStatistic;

            var count = 0;
            if (reproductionStat.ReproducedWithOrganelle.TryGetValue(organelle, out var data))
                count = CalculateCount(data);

            var formattedOrganelleName = MinimumCount <= 1 ?
                new LocalizedString("ORGANELLE_SINGULAR", organelle.Name) :
                new LocalizedString("ORGANELLE_PLURAL", organelle.Name);

            if (InARow)
            {
                builder.Append(new LocalizedString("UNLOCK_CONDITION_REPRODUCED_WITH_IN_A_ROW",
                    formattedOrganelleName, MinimumCount, Generations, count));
            }
            else
            {
                builder.Append(new LocalizedString("UNLOCK_CONDITION_REPRODUCED_WITH",
                    formattedOrganelleName, MinimumCount, Generations, count));
            }
        }

        public override bool Satisfied(IUnlockStateDataSource data1)
        {
            if (data1 is not WorldStatsTracker tracker)
                return false;

            var reproductionStat = tracker.PlayerReproductionStatistic;

            if (!reproductionStat.ReproducedWithOrganelle.TryGetValue(organelle, out var data))
                return false;

            return CalculateCount(data) >= Generations;
        }

        public override void Check(string name)
        {
            if (string.IsNullOrEmpty(RawOrganelle))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Organelle is empty");
            }

            if (MinimumCount < 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "MinimumCount cannot be less than one");
            }

            if (Generations < 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Generations cannot be less than one");
            }
        }

        public override void Resolve(SimulationParameters parameters)
        {
            organelle = parameters.GetOrganelleType(RawOrganelle);
        }

        private int CalculateCount(ReproductionStatistic.ReproductionOrganelleData data)
        {
            if (MinimumCount > 1)
            {
                if (InARow)
                {
                    var list = data.CountInGenerations;
                    int old = Math.Max(list.Count - Generations, 0);
                    var elements = list.Skip(old).Reverse().TakeWhile(c => c >= MinimumCount);

                    return elements.Count();
                }

                return data.CountInGenerations.Count(c => c >= MinimumCount);
            }

            return InARow ? data.GenerationsInARow : data.TotalGenerations;
        }
    }
}
