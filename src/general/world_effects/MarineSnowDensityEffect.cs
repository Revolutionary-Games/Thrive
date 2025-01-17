using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Adjusts marine snow densities in patches to correlate with populations of species (so that it makes sense for
///   it to appear only after there are microbes that are assumed to have died to create the marine snow)
/// </summary>
public class MarineSnowDensityEffect : IWorldEffect
{
    private const string MarineSnowConfiguration = "marineSnow";
    private const string TemplateBiomeForMarineSnow = "mesopelagic";
    private const float DepthDifferenceForMarineSnowFalling = 10;
    private const float MarineSnowPatchBottomExtraForFalling = 200;

    private readonly HashSet<OrganelleDefinition> availableOrganelles = new();

    [JsonProperty]
    private GameWorld targetWorld;

    public MarineSnowDensityEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        var templateBiome = SimulationParameters.Instance.GetBiome(TemplateBiomeForMarineSnow);
        var templateMarineSnowConfig = templateBiome.Conditions.Chunks[MarineSnowConfiguration];

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (patch.Biome.Chunks.TryGetValue(MarineSnowConfiguration, out var snowConfig))
            {
                // Found a patch with marine snow in it, update the density and available organelles
                AdjustMarineSnowDensity(patch, snowConfig, templateMarineSnowConfig);
            }
        }
    }

    private void AdjustMarineSnowDensity(Patch patch, ChunkConfiguration snowConfig, ChunkConfiguration templateConfig)
    {
        long totalPopulation = 0;

        totalPopulation = GetPopulationFromPatch(patch, totalPopulation);

        // To avoid extra list allocations, first inspect if something needs to change
        bool requiresMeshChanges = false;
        bool hashAtLeastOneOrganelle = false;

        // Existing ones that need to be removed
        foreach (var configMesh in snowConfig.Meshes)
        {
            if (IsMeshAllowed(configMesh, availableOrganelles))
            {
                hashAtLeastOneOrganelle = true;
            }
            else
            {
                // Need to remove this
                requiresMeshChanges = true;
            }
        }

        // New ones that need to be added
        foreach (var configMesh in templateConfig.Meshes)
        {
            if (IsMeshAllowed(configMesh, availableOrganelles))
            {
                hashAtLeastOneOrganelle = true;
            }
            else
            {
                // Need to add this
                requiresMeshChanges = true;
            }
        }

        // In case there are no allowed organelles to be chunks, force the population to zero
        if (!hashAtLeastOneOrganelle)
            totalPopulation = 0;

        // And finally perform the modification
        if (requiresMeshChanges && totalPopulation > 0)
        {
            var newMeshes = new List<ChunkConfiguration.ChunkScene>();

            // Template always must have all meshes, so we can use it to populate the new mesh list and not need to
            // read snowConfig at all here
            foreach (var configMesh in templateConfig.Meshes)
            {
                if (IsMeshAllowed(configMesh, availableOrganelles))
                {
                    newMeshes.Add(configMesh);
                }
            }

            snowConfig.Meshes = newMeshes;
        }

        availableOrganelles.Clear();

        // Finally, adjust the density and reassign the struct to apply the changes
        snowConfig.Density = CalculateDensity(totalPopulation);

        // This is left here commented out for further tweaking of the algorithm for calculating marine snow amounts
        // GD.Print("Marine snow density for patch ", patch.Name, ": ", snowConfig.Density, " from population: ",
        //     totalPopulation);
        patch.Biome.Chunks[MarineSnowConfiguration] = snowConfig;
    }

    private long GetPopulationFromPatch(Patch patch, long totalPopulation)
    {
        foreach (var tuple in patch.SpeciesInPatch)
        {
            // For now, only microbe species affect what organelles are available
            if (tuple.Key is MicrobeSpecies microbeSpecies)
            {
                foreach (var organelle in microbeSpecies.Organelles)
                {
                    availableOrganelles.Add(organelle.Definition);
                }
            }

            totalPopulation += tuple.Value;
        }

        // Add population from patches above this to simulate marine snow falling down
        foreach (var adjacentPatch in patch.Adjacent)
        {
            // If either totally above this patch, or the bottom of this patch is way deeper, then consider
            // the other patch above this one
            // TODO: somehow check lateral distance?
            if ((patch.Depth[0] - adjacentPatch.Depth[0] >= DepthDifferenceForMarineSnowFalling &&
                    adjacentPatch.Depth[1] > patch.Depth[1]) || (adjacentPatch.Depth[0] <= patch.Depth[0] &&
                    patch.Depth[1] - adjacentPatch.Depth[1] > MarineSnowPatchBottomExtraForFalling))
            {
                totalPopulation += (long)(GetPopulationFromPatch(adjacentPatch, totalPopulation) *
                    Constants.MARINE_SNOW_LOSS_MULTIPLIER_PER_PATCH);
            }
        }

        return totalPopulation;
    }

    private float CalculateDensity(long population)
    {
        if (population < Constants.MINIMUM_MARINE_SNOW_POPULATION)
            return 0;

        if (population >= Constants.MAXIMUM_MARINE_SNOW_POPULATION)
            return Constants.MAXIMUM_MARINE_SNOW_DENSITY;

        // The math scales the population ranges on how far into the specific range the population is and multiplies
        // the range end density with that and finally adds the base density

        if (population >= Constants.NORMAL_HIGH_MARINE_SNOW_POPULATION)
        {
            return (Constants.MAXIMUM_MARINE_SNOW_DENSITY - Constants.NORMAL_HIGH_MARINE_SNOW_DENSITY) *
                (population - Constants.NORMAL_HIGH_MARINE_SNOW_POPULATION) /
                (Constants.MAXIMUM_MARINE_SNOW_POPULATION - Constants.NORMAL_HIGH_MARINE_SNOW_POPULATION) +
                Constants.NORMAL_HIGH_MARINE_SNOW_DENSITY;
        }

        return (Constants.NORMAL_HIGH_MARINE_SNOW_DENSITY - Constants.MINIMUM_MARINE_SNOW_DENSITY) * (population -
                Constants.MINIMUM_MARINE_SNOW_POPULATION) / (Constants.NORMAL_HIGH_MARINE_SNOW_POPULATION -
                Constants.MINIMUM_MARINE_SNOW_POPULATION) +
            Constants.MINIMUM_MARINE_SNOW_DENSITY;
    }

    private bool IsMeshAllowed(ChunkConfiguration.ChunkScene chunk, HashSet<OrganelleDefinition> allowedOrganelles)
    {
        foreach (var organelle in allowedOrganelles)
        {
            // We probably don't want to need to check for upgrades here as that would get really complicated, so we
            // just check the base visuals here
            if (organelle.MatchesMarineSnow(chunk))
                return true;
        }

        return false;
    }
}
