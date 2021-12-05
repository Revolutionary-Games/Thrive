using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Manages spawning and processing compound clouds
/// </summary>
public class CompoundCloudSystem : Node, ISaveLoadedTracked
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
    private float elapsed;

    /// <summary>
    ///   The cloud resolution of the first cloud
    /// </summary>
    [JsonIgnore]
    public int Resolution => clouds[0].Resolution;

    /// <summary>
    ///   The size of cloud subdivisions along X axis of the world.
    /// </summary>
    [JsonIgnore]
    public int CloudSubdivisionXExtent => Constants.CLOUD_X_EXTENT / Constants.CLOUD_SQUARES_PER_SIDE;

    /// <summary>
    ///   The size of cloud subdivisions along Y axis of the world.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Given that the square aspect is exploited afterwards,
    ///     the possibility to have rectangle here may break things.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public int CloudSubdivisionYExtent => Constants.CLOUD_Y_EXTENT / Constants.CLOUD_SQUARES_PER_SIDE;

    public bool IsLoadedFromSave { get; set; }

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

        if (!IsLoadedFromSave)
        {
            clouds.Clear();
        }

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

        // TODO: this should be changed to detect which clouds are safe to delete
        while (clouds.Count > neededCloudsAtOnePosition)
        {
            var cloud = clouds[clouds.Count - 1];
            RemoveChild(cloud);
            cloud.Free();
            clouds.Remove(cloud);
        }

        // TODO: if the compound types have changed since we saved, that needs to be handled
        if (IsLoadedFromSave)
        {
            foreach (var cloud in clouds)
            {
                // Re-init with potentially changed compounds
                // TODO: special handling is needed if the compounds actually changed
                cloud.Init(fluidSystem, cloud.Compounds[0], cloud.Compounds[1], cloud.Compounds[2],
                    cloud.Compounds[3]);

                // Re-add the clouds as our children
                AddChild(cloud);
            }

            return;
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
    ///   Takes compound at world position
    /// </summary>
    /// <param name="compound">The compound type to take</param>
    /// <param name="worldPosition">World position to take from</param>
    /// <param name="fraction">The fraction of compound to take. Should be &lt;= 1</param>
    public float TakeCompound(Compound compound, Vector3 worldPosition, float fraction)
    {
        foreach (var cloud in clouds)
        {
            if (cloud.ContainsPosition(worldPosition, out var x, out var y))
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
            if (cloud.ContainsPosition(worldPosition, out var x, out var y))
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
    public void GetAllAvailableAt(Vector3 worldPosition, Dictionary<Compound, float> result)
    {
        foreach (var cloud in clouds)
        {
            if (cloud.ContainsPosition(worldPosition, out var x, out var y))
            {
                cloud.GetCompoundsAt(x, y, result);
            }
        }
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
        Dictionary<Compound, float> totals, float delta, float rate)
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

            cloud.ConvertToCloudLocal(position, out var cloudRelativeX, out var cloudRelativeY);

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
    ///   Tries to find specified compound as close to the point as possible.
    /// </summary>
    /// <param name="position">Position to search around</param>
    /// <param name="compound">What compound to search for</param>
    /// <param name="searchRadius">How wide to search around the point</param>
    /// <param name="minConcentration">Limits search to only find concentrations higher than this</param>
    /// <returns>The nearest found point for the compound or null</returns>
    public Vector3? FindCompoundNearPoint(Vector3 position, Compound compound, float searchRadius = 200,
        float minConcentration = 120)
    {
        if (searchRadius < 1)
            throw new ArgumentException("searchRadius must be >= 1");

        int resolution = Resolution;

        // This version is used when working with cloud local coordinates
        float localRadius = searchRadius / resolution;

        float localRadiusSquared = Mathf.Pow(searchRadius / resolution, 2);

        float nearestDistanceSquared = float.MaxValue;

        Vector3? closestPoint = null;

        foreach (var cloud in clouds)
        {
            // Skip clouds that don't handle the target compound
            if (!cloud.HandlesCompound(compound))
                continue;

            // Skip clouds that are out of range
            if (!cloud.ContainsPositionWithRadius(position, searchRadius))
                continue;

            cloud.ConvertToCloudLocal(position, out var cloudRelativeX, out var cloudRelativeY);

            // For simplicity all points within a bounding box around the
            // relative origin point is calculated and that is restricted by
            // checking if the point is within the circle before grabbing

            // And apparently there isn't an algorithm to generate the points with closest ones first? so
            // this goes through everything and keeps the closest found point

            int xEnd = (int)Mathf.Round(cloudRelativeX + localRadius);
            int yEnd = (int)Mathf.Round(cloudRelativeY + localRadius);

            for (int x = (int)Mathf.Round(cloudRelativeX - localRadius);
                x <= xEnd;
                x += 1)
            {
                for (int y = (int)Mathf.Round(cloudRelativeY - localRadius);
                    y <= yEnd;
                    y += 1)
                {
                    // Negative coordinates are always outside the cloud area
                    if (x < 0 || y < 0)
                        continue;

                    // Circle check
                    if (Mathf.Pow(x - cloudRelativeX, 2) +
                        Mathf.Pow(y - cloudRelativeY, 2) >
                        localRadiusSquared)
                    {
                        // Not in it
                        continue;
                    }

                    // Then just need to check that it is within the cloud simulation array
                    if (x < cloud.Size && y < cloud.Size)
                    {
                        if (cloud.AmountAvailable(compound, x, y) >= minConcentration)
                        {
                            // Potential target point
                            var currentWorldPos = cloud.ConvertToWorld(x, y);
                            var distance = (position - currentWorldPos).LengthSquared();

                            if (distance < nearestDistanceSquared)
                            {
                                closestPoint = currentWorldPos;
                                nearestDistanceSquared = distance;
                            }
                        }
                    }
                }
            }
        }

        return closestPoint;
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
    public void ReportPlayerPosition(Vector3 playerPositionInWorld)
    {
        // Calculate what our center should be
        var targetCenter = CalculateGridCenterForPlayerPos(playerPositionInWorld);

        // TODO: because we no longer check if the player has moved at least a bit
        // it is possible that this gets triggered very often if the player spins
        // around a cloud edge.

        if (!cloudGridCenter.Equals(targetCenter))
        {
            cloudGridCenter = targetCenter;
            PositionClouds();
        }
    }

    /// <summary>
    ///   Computes the coordinates of a given position on an infinite cloud subdivision grid of squares/rectangles.
    /// </summary>
    [SuppressMessage("ReSharper", "PossibleLossOfFraction",
        Justification = "I'm not sure how I should fix this code I didn't write (hhyyrylainen)")]
    private Vector3 CalculateGridCenterForPlayerPos(Vector3 pos)
    {
        // The gaps between the positions is used for calculations here. Otherwise
        // all clouds get moved when the player moves
        return new Vector3(
            (int)Math.Round(pos.x / CloudSubdivisionXExtent),
            0,
            (int)Math.Round(pos.z / CloudSubdivisionYExtent));
    }

    /// <summary>
    ///   Repositions all clouds according to the center of the cloud grid
    /// </summary>
    private void PositionClouds()
    {
        foreach (var cloud in clouds)
        {
            // TODO: make sure the cloud knows where we moved.
            // TODO: here we assume divisions to be squares...
            cloud.Translation = cloudGridCenter * CloudSubdivisionYExtent;
            cloud.UpdatePosition(new Int2((int)cloudGridCenter.x, (int)cloudGridCenter.z));
        }
    }

    private void UpdateCloudContents(float delta)
    {
        // The diffusion rate seems to have a bigger effect
        delta *= Constants.CLOUD_OPERATIONS_SPEED_FACTOR;

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
