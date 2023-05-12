using Godot;
using Newtonsoft.Json;

/// <summary>
///   Updates game logic for planet entities
/// </summary>
public class PlanetSystem
{
    private readonly Node worldRoot;

    [JsonProperty]
    private float elapsed = 1;

    public PlanetSystem(Node root)
    {
        worldRoot = root;
    }

    /// <summary>
    ///   The total amount of storage in all planet. Updated only once <see cref="Process"/> has been called.
    ///   TODO: switch to planet specific storage (with the overall HUD showing the aggregate stats)
    /// </summary>
    [JsonIgnore]
    public float CachedTotalStorage { get; private set; }

    [JsonIgnore]
    public int CachedTotalPopulation { get; private set; }

    public void Process(float delta, ISocietyStructureDataAccess societyData)
    {
        elapsed += delta;

        if (elapsed < Constants.SPACE_STAGE_PLANET_PROCESS_INTERVAL)
        {
            return;
        }

        float storage = 0;
        int population = 0;

        foreach (var planet in worldRoot.GetChildrenToProcess<PlacedPlanet>(Constants.PLANET_ENTITY_GROUP))
        {
            if (planet.ColonyStatus == PlacedPlanet.ColonizationState.NotColonized)
                continue;

            if (planet.ColonyStatus == PlacedPlanet.ColonizationState.ColonyBuilding)
            {
                // TODO: planet colonizing animation or something
                continue;
            }

            // TODO: processing for non-player cities
            if (planet.IsPlayerOwned)
            {
                planet.ProcessSpace(elapsed, societyData.SocietyResources);

                planet.ProcessResearch(elapsed, societyData);

                storage += planet.TotalStorageSpace;
                population += planet.Population;
            }
        }

        elapsed = 0;
        CachedTotalStorage = storage;
        CachedTotalPopulation = population;
    }

    /// <summary>
    ///   Immediately calculates the derived property values in this class
    /// </summary>
    public void CalculateDerivedStats()
    {
        float storage = 0;
        int population = 0;

        foreach (var planet in worldRoot.GetChildrenToProcess<PlacedPlanet>(Constants.PLANET_ENTITY_GROUP))
        {
            if (!planet.IsPlayerOwned || planet.ColonyStatus != PlacedPlanet.ColonizationState.Colonized)
                continue;

            storage += planet.TotalStorageSpace;
            population += planet.Population;
        }

        CachedTotalStorage = storage;
        CachedTotalPopulation = population;
    }
}
