using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;

// Copied over old documentation on how the clouds work
/*
The world is split into grid cells sizes of CLOUD_WIDTH x CLOUD_HEIGHT
where width is on the x axis and height is on the y axis.

The cloud entities are dynamically created around the player and there
can be multiple at the same place as each cloud can only have 4 types
of compound in it.

The Y coordinate of clouds is -5 to make them appear behind all cells.

The first cloud entity (at origin) is placed like this:

\todo This shows up really badly in doxygen


UV: 0, 0
-100, 0, 100                        100, 0, -100
    +-----------------+------------------+
    |               0, -100              |
    |                                    |
    |        -20, -40                    |
    |          x                         |
    |                                    |
    |                                    |
    | -100, 0       0, 0           100, 0|
    +                 x                  +
    |          m_cloudPos                |
    |                                    |
    |                                    |
    |                                    |
    |                                    |
    |               0, 100               |
    |                                    |
    +-----------------+------------------+
-100, 0, 100                        100, 0, 100
                                     UV: 1, 1

So the first cloud is at 0, 0 and then the next cloud to the right is
at (CLOUD_WIDTH * 2) 200, 0


World coordinates can be transformed to cloud coordinates by first
dividing both by the size of cloud in that direction (CLOUD_WIDTH,
CLOUD_HEIGHT) For example the point 25, 0, 70 (all future positions
will just show X and Z of the world coordinate):

27 / 100 = 0.25 and 70 / 100 = 0.7 and then floor():ing and casting to
integer to get the index of the cloud you get the grid index.

In the real code for simplicity we do a bounding box check with the
coordinates to determine in which cloud it is (this is slightly slower
but easier to reason about and with only about 20 cloud entities
existing at once the performance difference is negligible.

The bottom and right edges are part of the next cloud over.

This is implemented in \ref CompoundCloudSystem::cloudContainsPosition

Once we have a cloud selected (by selecting one with suitable
component ids matching the coordinates) we can translate the grab or
put operation to local cloud coordinates to perform it.

The cloud is for performance reasons split into less vector elements
than the actual size determined with CLOUD_RESOLUTION in order to have
to simulate less of the cloud cells interacting with each other.

So the local cloud coordinates are between 0-(CLOUD_SIMULATION_WIDTH - 1),
0-(CLOUD_SIMULATION_HEIGHT - 1).

To convert world coordinates into these the following math is used:

topLeftRelative.x = worldPos.x - (m_cloudPos.x - CLOUD_WIDTH)
topLeftRelative.z = worldPos.z - (m_cloudPos.z - CLOUD_HEIGHT)



topLeftRelative must always have x and z >= 0, otherwise the world
point was not within the cloud! and it has to be < CLOUD_WIDTH / CLOUD_HEIGHT


cloudLocal = topLeftRelative / CLOUD_RESOLUTION

cloudLocal must be < CLOUD_SIMULATION_WIDTH otherwise it is out of range.

With this method both putting compounds and getting compounds from a
cloud calculates the right coordinates.

This is implemented in \ref CompoundCloudSystem::convertWorldToCloudLocal

Example:

worldPos = -20, -40

topLeftRelative.x = -20 - (0 - 100) = 80
topLeftRelative.z = -40 - (0 - 100) = 60

These match all the conditions. So the final result is:

cloudLocal.X = 80 / 2 = 40
cloudLocal.Y = 60 / 2 = 30


Look at the tests for the clouds for more examples


When the cloud is rendered the densities from the simulation vector is
transferred to the texture and the top left is UV coordinate 0, 0 and
the bottom right is 1, 1.

*/

/// <summary>
///   Manages spawning and processing compound clouds
/// </summary>
public class CompoundCloudSystem : Node
{
    [JsonProperty]
    private int neededCloudsAtOnePosition;

    [JsonProperty]
    private List<CompoundCloudPlane> clouds = new List<CompoundCloudPlane>();

    private PackedScene cloudScene;

    private List<Compound> allCloudCompounds;

    /// <summary>
    ///   This is the point in the center of the middle cloud. This is
    ///   used for calculating which clouds to move when the player
    ///   moves.
    /// </summary>
    [JsonProperty]
    private Vector3 cloudGridCenter;

    [JsonProperty]
    private float elapsed = 0.0f;

    /// <summary>
    ///   The cloud resolution of the first cloud
    /// </summary>
    [JsonIgnore]
    public int Resolution
    {
        get { return clouds[0].Resolution; }
    }

    public override void _Ready()
    {
        cloudScene = GD.Load<PackedScene>("res://src/microbe_stage/CompoundCloudPlane.tscn");
    }

    public override void _Process(float delta)
    {
        elapsed += delta;

        // Limit the rate at which the clouds are processed as they
        // are a major performance sink
        if (elapsed >= Settings.Instance.CloudUpdateInterval)
        {
            UpdateCloudContents(elapsed);
            elapsed = 0.0f;
        }
    }

    /// <summary>
    ///   Resets the cloud contents and positions as well as the compound types they store
    /// </summary>
    public void Init(FluidSystem fluidSystem)
    {
        allCloudCompounds = SimulationParameters.Instance.GetCloudCompounds();

        clouds.Clear();

        // Count the number of clouds needed at one position from the loaded compound types
        neededCloudsAtOnePosition = (int)Math.Ceiling(allCloudCompounds.Count /
            (float)Constants.CLOUDS_IN_ONE);

        // We need to dynamically spawn more / delete some if this doesn't match
        while (clouds.Count < neededCloudsAtOnePosition)
        {
            var createdCloud = (CompoundCloudPlane)cloudScene.Instance();
            clouds.Add(createdCloud);
            AddChild(createdCloud);
        }

        while (clouds.Count > neededCloudsAtOnePosition)
        {
            var cloud = clouds[0];
            RemoveChild(cloud);
            cloud.Free();
            clouds.Remove(cloud);
        }

        for (int i = 0; i < clouds.Count; ++i)
        {
            Compound cloud1;
            Compound cloud2 = null;
            Compound cloud3 = null;
            Compound cloud4 = null;

            int startOffset = (i % neededCloudsAtOnePosition) * Constants.CLOUDS_IN_ONE;

            cloud1 = allCloudCompounds[startOffset + 0];

            if (startOffset + 1 < allCloudCompounds.Count)
                cloud2 = allCloudCompounds[startOffset + 1];

            if (startOffset + 2 < allCloudCompounds.Count)
                cloud3 = allCloudCompounds[startOffset + 2];

            if (startOffset + 3 < allCloudCompounds.Count)
                cloud4 = allCloudCompounds[startOffset + 3];

            clouds[i].Init(fluidSystem, cloud1, cloud2, cloud3, cloud4);
            clouds[i].Translation = new Vector3(0, 0, 0);
        }
    }

    /// <summary>
    ///   Places specified amount of compound at position
    /// </summary>
    /// <returns>True when placing succeeded, false if out of range</returns>
    public bool AddCloud(Compound compound, float density, Vector3 worldPosition)
    {
        // Find the target cloud //
        foreach (var cloud in clouds)
        {
            if (cloud.ContainsPosition(worldPosition, out int x, out int y))
            {
                // Within cloud

                // Skip wrong types
                if (!cloud.HandlesCompound(compound))
                    continue;

                cloud.AddCloud(compound, density, x, y);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///   AddCloud but taking compound name as I couldn't figure out a
    ///   way to do this with generics.
    /// </summary>
    public bool AddCloud(string compound, float density, Vector3 worldPosition)
    {
        foreach (var cloud in clouds)
        {
            if (cloud.ContainsPosition(worldPosition, out int x, out int y))
            {
                if (!cloud.HandlesCompound(compound))
                    continue;

                cloud.AddCloud(compound, density, x, y);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///   Takes compound at world position
    /// </summary>
    /// <param name="compound">The compound type to take</param>
    /// <param name="worldPosition">World position to take from</param>
    /// <param name="fraction">The fraction of compound to take. Should be &lt;= 1</param>
    public float TakeCompound(Compound compound, Vector3 worldPosition, float fraction)
    {
        foreach (var cloud in clouds)
        {
            int x, y;
            if (cloud.ContainsPosition(worldPosition, out x, out y))
            {
                // Within cloud

                // Skip wrong types
                if (!cloud.HandlesCompound(compound))
                    continue;

                return cloud.TakeCompound(compound, x, y, fraction);
            }
        }

        return 0;
    }

    public float AmountAvailable(Compound compound, Vector3 worldPosition, float fraction)
    {
        foreach (var cloud in clouds)
        {
            int x, y;
            if (cloud.ContainsPosition(worldPosition, out x, out y))
            {
                // Within cloud

                // Skip wrong types
                if (!cloud.HandlesCompound(compound))
                    continue;

                return cloud.AmountAvailable(compound, x, y, fraction);
            }
        }

        return 0;
    }

    /// <summary>
    ///   Returns the total amount of all compounds at position
    /// </summary>
    public Dictionary<string, float> GetAllAvailableAt(Vector3 worldPosition)
    {
        var result = new Dictionary<string, float>();

        foreach (var cloud in clouds)
        {
            int x, y;
            if (cloud.ContainsPosition(worldPosition, out x, out y))
            {
                cloud.GetCompoundsAt(x, y, result);
            }
        }

        return result;
    }

    /// <summary>
    ///   Absorbs compounds from clouds into a bag
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: finding a way to add threading here probably helps quite a bit
    ///   </para>
    /// </remarks>
    public void AbsorbCompounds(Vector3 position, float radius, CompoundBag storage,
        Dictionary<string, float> totals, float delta, float rate)
    {
        // It might be fine to remove this check but this was in the old code
        if (radius < 1.0f)
        {
            GD.PrintErr("Grab radius < 1 is not allowed");
            return;
        }

        int resolution = Resolution;

        // This version is used when working with cloud local coordinates
        float localGrabRadius = radius / resolution;

        float localGrabRadiusSquared = Mathf.Pow(radius / resolution, 2);

        // Find clouds that are in range for absorbing
        foreach (var cloud in clouds)
        {
            // Skip clouds that are out of range
            if (!cloud.ContainsPositionWithRadius(position, radius))
                continue;

            int cloudRelativeX, cloudRelativeY;
            cloud.ConvertToCloudLocal(position, out cloudRelativeX, out cloudRelativeY);

            // Calculate all circle positions and grab from all the valid
            // positions

            // For simplicity all points within a bounding box around the
            // relative origin point is calculated and that is restricted by
            // checking if the point is within the circle before grabbing
            int xEnd = (int)Mathf.Round(cloudRelativeX + localGrabRadius);
            int yEnd = (int)Mathf.Round(cloudRelativeY + localGrabRadius);

            for (int x = (int)Mathf.Round(cloudRelativeX - localGrabRadius);
                x <= xEnd;
                x += 1)
            {
                for (int y = (int)Mathf.Round(cloudRelativeY - localGrabRadius);
                    y <= yEnd;
                    y += 1)
                {
                    // Negative coordinates are always outside the cloud area
                    if (x < 0 || y < 0)
                        continue;

                    // Circle check
                    if (Mathf.Pow(x - cloudRelativeX, 2) +
                        Mathf.Pow(y - cloudRelativeY, 2) >
                        localGrabRadiusSquared)
                    {
                        // Not in it
                        continue;
                    }

                    // Then just need to check that it is within the cloud simulation array
                    if (x < cloud.Size && y < cloud.Size)
                    {
                        // Absorb all compounds in the cloud
                        cloud.AbsorbCompounds(x, y, storage, totals, delta, rate);
                    }
                }
            }
        }
    }

    /// <summary>
    ///   Clears the contents of all clouds
    /// </summary>
    public void EmptyAllClouds()
    {
        foreach (var cloud in clouds)
            cloud.ClearContents();
    }

    /// <summary>
    ///   Used from the stage to update the player position to reposition the clouds
    /// </summary>
    public void ReportPlayerPosition(Vector3 position)
    {
        // Calculate what our center should be
        var targetCenter = CalculateGridCenterForPlayerPos(position);

        // TODO: because we no longer check if the player has moved at least a bit
        // it is possible that this gets triggered very often if the player spins
        // around a cloud edge.

        if (!cloudGridCenter.Equals(targetCenter))
        {
            cloudGridCenter = targetCenter;
            PositionClouds();
        }
    }

    public void ApplyPropertiesFromSave(CompoundCloudSystem compoundCloudSystem)
    {
        cloudGridCenter = compoundCloudSystem.cloudGridCenter;
        elapsed = compoundCloudSystem.elapsed;

        // Copy concentrations (and as well as the other cloud parameters that need to be set)
        // TODO: allow saves to work if new compounds are added
        if (clouds.Count != compoundCloudSystem.clouds.Count)
            throw new Exception("Loading a save that has different compound cloud types doesn't currently work");

        for (int i = 0; i < clouds.Count; ++i)
        {
            // TODO: it's not very nice to pass null as the context here
            clouds[i].ApplySave(compoundCloudSystem.clouds[i], null);
        }
    }

    private static Vector3
        CalculateGridCenterForPlayerPos(Vector3 pos)
    {
        // The gaps between the positions is used for calculations here. Otherwise
        // all clouds get moved when the player moves
        return new Vector3(
            (int)Math.Round(pos.x / (Constants.CLOUD_X_EXTENT / 3)),
            0,
            (int)Math.Round(pos.z / (Constants.CLOUD_Y_EXTENT / 3)));
    }

    /// <summary>
    ///   Repositions all clouds according to the center of the cloud grid
    /// </summary>
    private void PositionClouds()
    {
        foreach (var cloud in clouds)
        {
            // TODO: make sure the cloud knows where we moved.
            cloud.Translation = cloudGridCenter * Constants.CLOUD_Y_EXTENT / 3;
            cloud.UpdatePosition(new Int2((int)cloudGridCenter.x, (int)cloudGridCenter.z));
        }
    }

    private void UpdateCloudContents(float delta)
    {
        // Do moving compounds on the edges of the clouds serially
        foreach (var cloud in clouds)
        {
            cloud.UpdateEdgesBeforeCenter(delta);
        }

        var executor = TaskExecutor.Instance;
        var tasks = new List<Task>(9 * neededCloudsAtOnePosition);

        foreach (var cloud in clouds)
        {
            cloud.QueueUpdateCloud(delta, tasks);
        }

        // Start and wait for tasks to finish
        executor.RunTasks(tasks);
        tasks.Clear();

        // Do moving compounds on the edges of the clouds serially
        foreach (var cloud in clouds)
        {
            cloud.UpdateEdgesAfterCenter(delta);
        }

        // Update the cloud textures in parallel
        foreach (var cloud in clouds)
        {
            cloud.QueueUpdateTextureImage(tasks);
        }

        executor.RunTasks(tasks);

        foreach (var cloud in clouds)
        {
            cloud.UpdateTexture();
        }
    }
}
