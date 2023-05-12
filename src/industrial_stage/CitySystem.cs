using Godot;
using Newtonsoft.Json;

/// <summary>
///   Updates city entities
/// </summary>
public class CitySystem
{
    private readonly Node worldRoot;

    [JsonProperty]
    private float elapsed = 1;

    public CitySystem(Node root)
    {
        worldRoot = root;
    }

    /// <summary>
    ///   The total amount of storage in all cities. Updated only once <see cref="Process"/> has been called.
    ///   TODO: switch to city specific storage (with some idea on how to handle building new cities resource-wise)
    /// </summary>
    [JsonIgnore]
    public float CachedTotalStorage { get; private set; }

    [JsonIgnore]
    public int CachedTotalPopulation { get; private set; }

    /// <summary>
    ///   Used to detect when any of the player's cities has built a spacecraft
    /// </summary>
    [JsonIgnore]
    public (PlacedCity City, UnitType Spacecraft)? FirstLaunchableSpacecraft { get; private set; }

    public void Process(float delta, ISocietyStructureDataAccess societyData)
    {
        elapsed += delta;

        if (elapsed < Constants.INDUSTRIAL_STAGE_CITY_PROCESS_INTERVAL)
        {
            // TODO: hopefully this scales performance-wise well enough to not cause problems when there are a bunch
            // of cities
            foreach (var city in worldRoot.GetChildrenToProcess<PlacedCity>(Constants.CITY_ENTITY_GROUP))
            {
                // Smoothly update build queue bars
                city.ProcessBuildQueueProgressValues(delta);
            }

            return;
        }

        float storage = 0;
        int population = 0;
        FirstLaunchableSpacecraft = null;

        foreach (var city in worldRoot.GetChildrenToProcess<PlacedCity>(Constants.CITY_ENTITY_GROUP))
        {
            if (!city.Completed || !city.IsPlayerCity)
            {
                // TODO: city forming animation or something
                continue;
            }

            // TODO: processing for non-player cities

            city.ProcessBuildQueueProgressValues(delta);

            // The following is pretty quick prototype code, there's probably better way to implement this
            city.ProcessIndustrial(elapsed, societyData.SocietyResources);

            if (city.IsPlayerCity)
            {
                city.ProcessResearch(elapsed, societyData);

                storage += city.TotalStorageSpace;
                population += city.Population;

                foreach (var unit in city.GarrisonedUnits)
                {
                    if (unit.HasSpaceMovement)
                    {
                        FirstLaunchableSpacecraft ??= (city, unit);
                    }
                }
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

        foreach (var city in worldRoot.GetChildrenToProcess<PlacedCity>(Constants.CITY_ENTITY_GROUP))
        {
            if (!city.Completed || !city.IsPlayerCity)
                continue;

            storage += city.TotalStorageSpace;
            population += city.Population;
        }

        CachedTotalStorage = storage;
        CachedTotalPopulation = population;
    }

    public void ClearLaunchableSpacecraft()
    {
        FirstLaunchableSpacecraft = null;
    }
}
