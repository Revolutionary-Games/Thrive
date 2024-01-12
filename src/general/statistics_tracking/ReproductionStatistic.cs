using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        // Due to needing to track how many generations in a row organelle was in the player's species, all organelles
        // (even ones that aren't currently added) need to be processed
        foreach (var definition in SimulationParameters.Instance.GetAllOrganelles())
        {
            if (!ReproducedWithOrganelle.TryGetValue(definition, out var data))
            {
                data = ReproducedWithOrganelle[definition] = new ReproductionOrganelleData();
            }

            data.IncrementBy(CountOrganellesOfType(definition, organelles));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CountOrganellesOfType(OrganelleDefinition definition, OrganelleLayout<PlacedOrganelle> layout)
    {
        int count = 0;

        foreach (var organelle in layout.Organelles)
        {
            // As the player grows before reproducing, this might end up double counting organelles so this skip is
            // here
            if (organelle.IsDuplicate)
                continue;

            if (organelle.Definition == definition)
                ++count;
        }

        return count;
    }

    /// <summary>
    ///   Contains data about how many times the player has reproduced with an organelle
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
        /// <remarks>
        ///   <para>
        ///     TODO: some kind of upper limit might be nice here to not just add more and more data that accumulates
        ///     here
        ///   </para>
        /// </remarks>
        [JsonProperty]
        public List<int> CountInGenerations { get; private set; } = new();

        public void IncrementBy(int count)
        {
            if (count <= 0)
            {
                // A generation without this organelle
                GenerationsInARow = 0;
                CountInGenerations.Add(0);
                return;
            }

            CountInGenerations.Add(count);
            ++GenerationsInARow;
            ++TotalGenerations;
        }
    }
}
