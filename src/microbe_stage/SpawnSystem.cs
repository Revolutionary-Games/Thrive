using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Holds some data for different spawn types, such as AI cells, floating chunks, etc.
/// </summary>
public struct SpawnType
{
	public int spawnRadius;
	public int spawnRadiusSqr;
	public int spawnFrequency;
	public Func<Node, Vector3, Node> factoryFunction;
	public int id;
}

/// <summary>
///   A component for a Spawn reactive entity
/// </summary>
public class Spawned : Node
{
	public int spawnRadiusSqr;

	public Spawned(int newSpawnRadiusSqr)
	{
		spawnRadiusSqr = newSpawnRadiusSqr;
		Name = "Spawned";
	}
}

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
{
    /// <summary>
    ///   Sets how often the spawn system runs and checks things
    /// </summary>
    private float interval = 1.0f;

    private float elapsed = 0.0f;

    private Node worldRoot;

	private int nextId = 0;

	private Dictionary<int, SpawnType> spawnTypes = new Dictionary<int, SpawnType>();

	private Vector3 previousPlayerPosition = new Vector3();

	public SpawnSystem(Node root)
    {
        worldRoot = root;
	}

    // Adds a spawn type
	public int AddSpawnType(Func<Node, Vector3, Node> factoryFunction, int spawnDensity, int spawnRadius)
	{
		SpawnType newSpawnType = new SpawnType();
		newSpawnType.factoryFunction = factoryFunction;
        newSpawnType.spawnRadius = spawnRadius;
        newSpawnType.spawnRadiusSqr = spawnRadius * spawnRadius;
        newSpawnType.spawnFrequency = spawnDensity * newSpawnType.spawnRadiusSqr * 4;
		newSpawnType.id = nextId;
		nextId++;
		spawnTypes.Add(newSpawnType.id, newSpawnType);
		return newSpawnType.id;
	}

    // Removes a spawn type
	public void RemoveSpawnType(int id)
	{
		spawnTypes.Remove(id);
	}

    // Updates the density of a spawner
    public bool UpdateDensity(int spawnId, int spawnDensity)
    {
        if (!spawnTypes.ContainsKey(spawnId))
            return false;

        var found = spawnTypes[spawnId];
        found.spawnFrequency = spawnDensity * found.spawnRadiusSqr * 4;
        return true;
    }

	// Clears the spawners
	public void Clear()
	{
		GD.Print("Clearing spawn system spawners");
		spawnTypes.Clear();
		previousPlayerPosition = new Vector3();
		elapsed = 0;
	}

	// Processes spawning and despawning things
	public void Process(float delta)
    {
        elapsed += delta;

        while (elapsed >= interval)
        {
            elapsed -= interval;
            // Getting the player position.
			var playerPosition = ((MicrobeStage)worldRoot.GetParent().GetParent()).Player.GlobalTransform.origin;
            // Remove the y-position from player position
			playerPosition.y = 0;
			int entitiesDeleted = 0;

			// Despawn entities
			foreach (Node entity in worldRoot.GetChildren())
			{
                // delete a max of two entities per step to reduce lag from deleting
                // tons of entities at once
				if (entitiesDeleted < 2)
				{
					if (entity.HasNode("Spawned"))
					{
                        Spawned spawned = entity.GetNode<Spawned>("Spawned");
                        var entityPosition = ((Spatial)entity).GlobalTransform.origin;
                        var squaredDistance = (playerPosition - entityPosition).LengthSquared();

                        // If the entity is too far away from the player, despawn it.
                        if (squaredDistance > spawned.spawnRadiusSqr)
                        {
                            entitiesDeleted++;
                            entity.QueueFree();
                        }
                    }
				}
				else
				{
                    // get out of loop if you hit max
					break;
				}
			}

			Random random = new Random();

			// Spawn new entities.
			foreach (var st in spawnTypes)
			{
                /*
                To actually spawn a given entity for a given attempt, two
                conditions should be met.  The first condition is a random
                chance that adjusts the spawn frequency to the approprate
                amount. The second condition is whether the entity will
                spawn in a valid position.  It is checked when the first
                condition is met and a position for the entity has been
                decided.

                To allow more than one entity of each type to spawn per
                spawn cycle, the SpawnSystem attempts to spawn each given
                entity multiple times depending on the spawnFrequency.
                numAttempts stores how many times the SpawnSystem attempts
                to spawn the given entity.
                */
				SpawnType spawnType = st.Value;
				int numAttempts = Math.Max(spawnType.spawnFrequency * 2, 1);

				for (int i = 0; i < numAttempts; i++)
				{
					if (random.Next(0, numAttempts) < spawnType.spawnFrequency)
					{
                        /*
                        First condition passed. Choose a location for the entity.

                        A random location in the square of sidelength 2*spawnRadius
                        centered on the player is chosen. The corners
                        of the square are outside the spawning region, but they
                        will fail the second condition, so entities still only
                        spawn within the spawning region.
                        */
                        float distanceX = (float)random.NextDouble() * spawnType.spawnRadius - (float)random.NextDouble() * spawnType.spawnRadius;
                        float distanceZ = (float)random.NextDouble() * spawnType.spawnRadius - (float)random.NextDouble() * spawnType.spawnRadius;

                        // Distance from the player.
                        Vector3 displacement = new Vector3(distanceX, 0, distanceZ);
                        float squaredDistance = displacement.LengthSquared();

                        // Distance from the location of the player in the previous
                        // spawn cycle.
                        Vector3 previousDisplacement = displacement + playerPosition - previousPlayerPosition;
                        float previousSquaredDistance = previousDisplacement.LengthSquared();

                        if(squaredDistance <= spawnType.spawnRadiusSqr && previousSquaredDistance > spawnType.spawnRadiusSqr)
                        {
                            // Second condition passed. Spawn the entity.
                            Node spawnedEntity = spawnType.factoryFunction(worldRoot, playerPosition + displacement);

                            // Giving the new entity a spawn component.
                            if(spawnedEntity != null)
                            {
                                try
                                {
									spawnedEntity.AddChild(new Spawned(spawnType.spawnRadiusSqr));
									worldRoot.AddChild(spawnedEntity);
								}
                                catch(Exception e)
                                {

                                    GD.PrintErr("SpawnSystem failed to add Spawned, exception:");
									GD.PrintErr(e.Message);
									GD.PrintErr(e.Source);
									GD.PrintErr(e.StackTrace);
								}
                            }
                        }
                    }
				}
			}
		}
    }

    /// <summary>
    ///   Prepares the spawn system for a new game
    /// </summary>
    public void Init()
    {
		// For testing
		AddSpawnType((worldRoot, position) =>
        {
            Microbe microbe = (Microbe)GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn").Instance();
			microbe.GlobalTranslate(position);
			return microbe;
        }, 70000, 150);
	}
}
