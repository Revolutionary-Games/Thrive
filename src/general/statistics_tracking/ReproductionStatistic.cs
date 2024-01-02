using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using Newtonsoft.Json;

public class ReproductionStatistic : IStatistic
{
    [JsonProperty]
    public int TimesReproduced { get; set; }

    [JsonProperty]
    public Dictionary<Biome, int> ReproducedInBiomes { get; set; } = new();

    [JsonProperty]
    public Dictionary<OrganelleDefinition, ReproductionOrganelleData> ReproducedWithOrganelle { get; set; } = new();

    public void RecordPlayerReproduction(in Entity player, Biome biome)
    {
        ++TimesReproduced;

        var organelles = player.Get<OrganelleContainer>().Organelles!;

        foreach (var defintion in SimulationParameters.Instance.GetAllOrganelles())
        {
            if (!ReproducedWithOrganelle.TryGetValue(defintion, out var data))
            {
                data = ReproducedWithOrganelle[defintion] = new ReproductionOrganelleData();
            }

            data!.IncrementBy(organelles.Count(o => o.Definition == defintion));
        }

        if (ReproducedInBiomes.TryGetValue(biome, out var value))
        {
            ReproducedInBiomes[biome] = ++value;
        }
        else
        {
            ReproducedInBiomes[biome] = 1;
        }
    }

    /// <summary>
    ///   Contains data about how many times the player has reporduced with an organelle
    /// </summary>
    public class ReproductionOrganelleData
    {
        /// <summary>
        ///   The total amount of generations the player evolved with this organelle
        /// </summary>
        [JsonProperty]
        public int TotalGenerations { get; private set; }

        /// <summary>
        ///   The amount of generations that the player evolved this organelle in a row
        /// </summary>
        [JsonProperty]
        public int GenerationsInARow { get; private set; }

        /// <summary>
        ///   The amount of this organelle the player evolved with in each generation
        /// </summary>
        [JsonProperty]
        public List<int> CountInGenerations { get; private set; } = new();

        public void IncrementBy(int count)
        {
            if (count <= 0)
            {
                GenerationsInARow = 0;
                CountInGenerations.Add(0);
                return;
            }

            CountInGenerations.Add(count);
            GenerationsInARow++;
            TotalGenerations++;
        }
    }
}
