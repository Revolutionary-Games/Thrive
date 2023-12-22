using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using Newtonsoft.Json;

public class ReproductionStatistic : IStatistic
{
    public StatsTrackerEvent Event { get; set; } = StatsTrackerEvent.PlayerReproduced;

    [JsonProperty]
    public int TimesReproduced { get; set; }

    public Dictionary<string, int> ReproducedInBiomes { get; set; } = new();

    public Dictionary<string, int> ReproducedWithOrganelle { get; set; } = new();

    public void RecordPlayerReporduction(in Entity player, Biome? biome)
    {
        TimesReproduced++;

        var organelles = player.Get<OrganelleContainer>().Organelles?.Organelles.ToHashSet();

        if (organelles != null)
        {
            foreach (var organelle in organelles)
            {
                var key = organelle.Definition.InternalName;

                if (ReproducedWithOrganelle.TryGetValue(key, out int value))
                {
                    ReproducedWithOrganelle[key] = ++value;
                }
                else
                {
                    ReproducedWithOrganelle[key] = 1;
                }
            }
        }

        if (biome != null)
        {
            if (ReproducedInBiomes.TryGetValue(biome.InternalName, out int value))
            {
                ReproducedInBiomes[biome.InternalName] = ++value;
            }
            else
            {
                ReproducedInBiomes[biome.InternalName] = 1;
            }
        }
    }
}