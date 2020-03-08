using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

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
    private readonly List<Task> tasks = new List<Task>();

    private int neededCloudsAtOnePosition;

    private List<CompoundCloudPlane> clouds = new List<CompoundCloudPlane>();
    private PackedScene cloudScene;

    private List<Compound> allCloudCompounds;

    /// <summary>
    ///   This is the point in the center of the middle cloud. This is
    ///   used for calculating which clouds to move when the player
    ///   moves.
    /// </summary>
    private Vector3 cloudGridCenter;

    /// <summary>
    ///   This is here to reuse this list
    /// </summary>
    private List<CompoundCloudPlane> tooFarAwayClouds = new List<CompoundCloudPlane>();

    public override void _Ready()
    {
        cloudScene = GD.Load<PackedScene>("res://src/microbe_stage/CompoundCloudPlane.tscn");
    }

    public override void _Process(float delta)
    {
        UpdateCloudContents(delta);
    }

    /// <summary>
    ///   Resets the cloud contents and positions as well as the compound types they store
    /// </summary>
    public void Init(FluidSystem fluidSystem)
    {
        allCloudCompounds = SimulationParameters.Instance.GetCloudCompounds();

        clouds.Clear();

        foreach (var child in GetChildren())
        {
            clouds.Add((CompoundCloudPlane)child);
        }

        // Count the number of clouds needed at one position from the loaded compound types
        neededCloudsAtOnePosition = (int)Math.Ceiling(allCloudCompounds.Count /
            (float)Constants.CLOUDS_IN_ONE);

        // We need to dynamically spawn more / delete some if this doesn't match
        while (clouds.Count < 9 * neededCloudsAtOnePosition)
        {
            var createdCloud = (CompoundCloudPlane)cloudScene.Instance();
            clouds.Add(createdCloud);
            AddChild(createdCloud);
        }

        while (clouds.Count > 9 * neededCloudsAtOnePosition)
        {
            var cloud = clouds[0];
            RemoveChild(cloud);
            cloud.Free();
            clouds.Remove(cloud);
        }

        cloudGridCenter = new Vector3(0, 0, 0);
        var positions = CalculateGridPositions(cloudGridCenter);

        int positionIndex = 0;
        int positionedCounter = 0;

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

            // Position the cloud taking into account how many clouds
            // need to be at the same position.
            // Doing this here makes the cloud reposition logic simpler.
            clouds[i].Translation = positions[positionIndex] - new Vector3(0, startOffset * 0.01f, 0);

            ++positionedCounter;

            if (positionedCounter == neededCloudsAtOnePosition)
            {
                positionedCounter = 0;
                ++positionIndex;
            }
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
    ///   Takes compound at world position
    /// </summary>
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

    private static Vector3[]
        CalculateGridPositions(Vector3 center)
    {
        return new Vector3[]
        {
            // Center
            center,

            // Top left
            center + new Vector3(-Constants.CLOUD_WIDTH * 2, 0, -Constants.CLOUD_HEIGHT * 2),

            // Up
            center + new Vector3(0, 0, -Constants.CLOUD_HEIGHT * 2),

            // Top right
            center + new Vector3(Constants.CLOUD_WIDTH * 2, 0, -Constants.CLOUD_HEIGHT * 2),

            // Left
            center + new Vector3(-Constants.CLOUD_WIDTH * 2, 0, 0),

            // Right
            center + new Vector3(Constants.CLOUD_WIDTH * 2, 0, 0),

            // Bottom left
            center + new Vector3(-Constants.CLOUD_WIDTH * 2, 0, Constants.CLOUD_HEIGHT * 2),

            // Down
            center + new Vector3(0, 0, Constants.CLOUD_HEIGHT * 2),

            // Bottom right
            center + new Vector3(Constants.CLOUD_WIDTH * 2, 0, Constants.CLOUD_HEIGHT * 2),
        };
    }

    private static Vector3
        CalculateGridCenterForPlayerPos(Vector3 pos)
    {
        // The gaps between the positions is used for calculations here. Otherwise
        // all clouds get moved when the player moves
        return new Vector3(
            (int)Math.Round(pos.x / Constants.CLOUD_X_EXTENT) * Constants.CLOUD_X_EXTENT,
            0,
            (int)Math.Round(pos.z / Constants.CLOUD_Y_EXTENT) * Constants.CLOUD_Y_EXTENT);
    }

    /// <summary>
    ///   Repositions all clouds according to the center of the cloud grid
    /// </summary>
    private void PositionClouds()
    {
        var positions = CalculateGridPositions(cloudGridCenter);

        tooFarAwayClouds.Clear();

        // All clouds that aren't at one of the requiredCloudPositions needs to
        // be moved. Also only one from each cloud group needs to be at each
        // position
        foreach (var cloud in clouds)
        {
            bool matched = false;

            // Check if it is at any of the valid positions
            foreach (var requiredPos in positions)
            {
                // An exact check might work but just to be safe slight
                // inaccuracy is allowed here
                if ((cloud.Translation - requiredPos).LengthSquared() < 0.01f)
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                tooFarAwayClouds.Add(cloud);
            }
        }

        // Move clouds that are too far away
        // We check through each position that should have a cloud and move one
        // where there isn't one. This also needs to take into account the cloud
        // groups

        // Loop through the cloud groups
        for (int c = 0; c < allCloudCompounds.Count; c += Constants.CLOUDS_IN_ONE)
        {
            var groupType = allCloudCompounds[c];

            // Loop for moving clouds to all needed positions for each group
            foreach (var requiredPos in positions)
            {
                bool hasCloud = false;

                foreach (var cloud in clouds)
                {
                    // An exact check might work but just to be safe slight
                    // inaccuracy is allowed here
                    if ((cloud.Translation - requiredPos).LengthSquared() < 0.01f)
                    {
                        // Check that the group of the cloud is correct
                        if (groupType == cloud.Compound1)
                        {
                            hasCloud = true;
                            break;
                        }
                    }
                }

                if (hasCloud)
                    continue;

                bool filled = false;

                // We need to find a cloud from the right group
                for (int checkReposition = 0; checkReposition < tooFarAwayClouds.Count;
                    ++checkReposition)
                {
                    if (tooFarAwayClouds[checkReposition] != null &&
                        tooFarAwayClouds[checkReposition].Compound1 ==
                            groupType)
                    {
                        // Found a candidate

                        // Move it
                        tooFarAwayClouds[checkReposition].RecycleToPosition(
                            requiredPos);

                        // Set to null to skip on next scan
                        tooFarAwayClouds[checkReposition] = null;

                        filled = true;
                        break;
                    }
                }

                if (!filled)
                {
                    GD.PrintErr("CompoundCloudSystem: Logic error in moving far clouds, " +
                        "didn't find any to use for needed pos");
                    break;
                }
            }
        }
    }

    private void UpdateCloudContents(float delta)
    {
        // Update the cloud textures
        // TODO: update the clouds in some positional order
        // (like top left -> top center -> top right).
        foreach (var cloud in clouds)
        {
            cloud.UpdateCloud(delta);
            cloud.UploadTexture();
        }
    }
}
