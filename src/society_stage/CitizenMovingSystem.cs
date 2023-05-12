using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Handles spawning and making citizens move around to appear busy to the player
/// </summary>
public class CitizenMovingSystem
{
    private readonly Node worldRoot;

    private readonly Random random = new();

    private PackedScene citizenScene = null!;

    private Species species = null!;

    private float elapsed;

    [JsonProperty]
    private float elapsedSinceSpawn = 1;

    [JsonProperty]
    private int lastCitizenCount;

    public CitizenMovingSystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
    }

    public void Init(Species playerSpecies)
    {
        species = playerSpecies;

        citizenScene = SpawnHelpers.LoadCitizenScene();
    }

    public void Process(float delta, long population)
    {
        elapsedSinceSpawn += delta;
        elapsed += delta;

        if (elapsedSinceSpawn > Constants.SOCIETY_STAGE_CITIZEN_SPAWN_INTERVAL)
        {
            elapsedSinceSpawn = 0;

            // TODO: make this math scale to large sizes a bit more sensibly
            SpawnCitizenIfUnderLimit((int)Math.Min(Math.Ceiling(population * 0.3f), 50));
            return;
        }

        if (elapsed < Constants.SOCIETY_STAGE_CITIZEN_PROCESS_INTERVAL)
            return;

        List<Vector3>? pointsOfInterest = null;

        elapsed = 0;
        lastCitizenCount = 0;

        foreach (var citizen in worldRoot.GetChildrenToProcess<SocietyCreature>(Constants.CITIZEN_GROUP))
        {
            ++lastCitizenCount;

            if (citizen.ExternallyControlled)
                continue;

            if (citizen.HasReachedGoal())
            {
                // TODO: random delay before moving again
                pointsOfInterest ??= GeneratePointsOfInterest();

                citizen.SetNewDestination(pointsOfInterest.Random(random));

                // TODO: citizens should sometimes "enter" a building and disappear for a while
            }
        }
    }

    private void SpawnCitizenIfUnderLimit(int wantedAmount)
    {
        if (lastCitizenCount >= wantedAmount)
            return;

        // Room to spawn a new citizen
        SpawnHelpers.SpawnCitizen(species, GetRandomSpawnPoint(), worldRoot, citizenScene);

        ++lastCitizenCount;
    }

    private List<Vector3> GeneratePointsOfInterest()
    {
        var result = new List<Vector3>();

        // Entrances of buildings
        // TODO: it would be nice to slowly update this list if there's a ton of buildings instead of needing to
        // calculate this each frame it is needed from scratch (or another random sampling approach would be nice)
        foreach (var structure in worldRoot.GetChildrenToProcess<PlacedStructure>(Constants.STRUCTURE_ENTITY_GROUP))
        {
            // Visiting incomplete buildings is also fine, so we don't check here for completeness

            var position = structure.GlobalTranslation;

            var offset = structure.RotatedExtraInteractionOffset();

            if (offset != null)
                position += offset.Value;

            result.Add(position);
        }

        // If there's just one or two buildings, add some points around the existing point
        if (result.Count < 4)
        {
            var first = result[0];

            for (int i = 0; i < 4; ++i)
            {
                result.Add(new Vector3(first.x + random.NextFloat() * 40, 0, first.z + random.NextFloat() * 40));
                result.Add(new Vector3(first.x - random.NextFloat() * 40, 0, first.z - random.NextFloat() * 40));
            }
        }

        // TODO: world resource points etc.

        return result;
    }

    /// <summary>
    ///   Gets a random point to spawn a citizen at, currently looks for buildings with housing
    /// </summary>
    private Vector3 GetRandomSpawnPoint()
    {
        // Find all structures with housing
        var potentialPoints = new List<Vector3>();

        foreach (var structure in worldRoot.GetChildrenToProcess<PlacedStructure>(Constants.STRUCTURE_ENTITY_GROUP))
        {
            if (!structure.Completed)
                continue;

            var housingComponent = structure.GetComponent<HousingComponent>();
            if (housingComponent != null)
            {
                var position = structure.GlobalTranslation;

                var offset = structure.RotatedExtraInteractionOffset();

                if (offset != null)
                    position += offset.Value;

                potentialPoints.Add(position);
            }
        }

        if (potentialPoints.Count < 1)
        {
            GD.PrintErr("Could not find spawn point for a citizen");
            return Vector3.Zero;
        }

        return potentialPoints.Random(new Random());
    }
}
