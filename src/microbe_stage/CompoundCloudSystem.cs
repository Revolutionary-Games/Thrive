using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using Systems;

[RuntimeCost(35)]
public partial class CompoundCloudSystem : Node, IReadonlyCompoundClouds, ISaveLoadedTracked
{
    [JsonProperty]
    private int neededCloudsAtOnePosition;

    [JsonProperty]
    private List<CompoundCloudPlane> clouds = new();

#pragma warning disable CA2213
    private PackedScene cloudScene = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private Vector3 cloudGridCenter;

    [JsonProperty]
    private double elapsed;

    [JsonIgnore]
    private float currentBrightness = 1.0f;

    [JsonIgnore]
    public int Resolution => clouds[0].Resolution;

    public bool IsLoadedFromSave { get; set; }

    public override void _Ready()
    {
        cloudScene = GD.Load<PackedScene>("res://src/microbe_stage/CompoundCloudPlane.tscn");
    }

    public void Init(FluidCurrentsSystem fluidSystem)
    {
        var allCloudCompounds = SimulationParameters.Instance.GetCloudCompounds();

        if (!IsLoadedFromSave)
        {
            clouds.Clear();
        }

        neededCloudsAtOnePosition = (int)Math.Ceiling(allCloudCompounds.Count / (float)Constants.CLOUDS_IN_ONE);

        while (clouds.Count < neededCloudsAtOnePosition)
        {
            var createdCloud = cloudScene.Instantiate<CompoundCloudPlane>();
            clouds.Add(createdCloud);
            AddChild(createdCloud);
        }

        while (clouds.Count > neededCloudsAtOnePosition)
        {
            var cloud = clouds[clouds.Count - 1];
            RemoveChild(cloud);
            cloud.Free();
            clouds.Remove(cloud);
        }

        int renderPriority = -1;

        if (IsLoadedFromSave)
        {
            foreach (var cloud in clouds)
            {
                cloud.Init(fluidSystem, renderPriority, cloud.Compounds[0]!, cloud.Compounds[1], cloud.Compounds[2],
                cloud.Compounds[3]);
                --renderPriority;
                AddChild(cloud);
            }

            return;
        }

        for (int i = 0; i < clouds.Count; ++i)
        {
            Compound cloud1;
            Compound? cloud2 = null;
            Compound? cloud3 = null;
            Compound? cloud4 = null;

            int startOffset = (i % neededCloudsAtOnePosition) * Constants.CLOUDS_IN_ONE;
            cloud1 = allCloudCompounds[startOffset + 0];

            if (startOffset + 1 < allCloudCompounds.Count)
                cloud2 = allCloudCompounds[startOffset + 1];

            if (startOffset + 2 < allCloudCompounds.Count)
                cloud3 = allCloudCompounds[startOffset + 2];

            if (startOffset + 3 < allCloudCompounds.Count)
                cloud4 = allCloudCompounds[startOffset + 3];

            clouds[i].Init(fluidSystem, renderPriority, cloud1, cloud2, cloud3, cloud4);
            --renderPriority;
            clouds[i].Position = new Vector3(0, 0, 0);
        }
    }

    public override void _Process(double delta)
    {
        elapsed += delta;

        if (elapsed >= Settings.Instance.CloudUpdateInterval)
        {
            UpdateCloudContents((float)elapsed);
            elapsed = 0;
        }
    }

    public bool AddCloud(Compound compound, float density, Vector3 worldPosition)
    {
        foreach (var cloud in clouds)
        {
            if (cloud.ContainsPosition(worldPosition, out int x, out int y))
            {
                if (cloud.AddCloudInterlockedIfHandlesType(compound, x, y, density))
                    return true;
            }
        }

        return false;
    }

    public float TakeCompound(Compound compound, Vector3 worldPosition, float fraction)
    {
        if (fraction < 0.0f)
            throw new ArgumentException("Fraction to take can't be negative");

        foreach (var cloud in clouds)
        {
            if (cloud.ContainsPosition(worldPosition, out var x, out var y))

            {
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
                if (!cloud.HandlesCompound(compound))
                    continue;

                return cloud.AmountAvailable(compound, x, y, fraction);
            }

        }

        return 0;
    }

    public void GetAllAvailableAt(Vector3 worldPosition, Dictionary<Compound, float> result, bool onlyAbsorbable = true)
    {
        foreach (var cloud in clouds)
        {
            if (cloud.ContainsPosition(worldPosition, out var x, out var y))
            {
                cloud.GetCompoundsAt(x, y, result, onlyAbsorbable);
            }
        }
    }

    public void AbsorbCompounds(Vector3 position, float radius, CompoundBag storage,
    Dictionary<Compound, float>? totals, float delta, float rate)
    {
        if (radius < 1.0f)
        {
            GD.PrintErr("Grab radius < 1 is not allowed");
            return;
        }

        int resolution = Resolution;
        float localGrabRadius = radius / resolution;
        float localGrabRadiusSquared = Mathf.Pow(radius / resolution, 2);

        foreach (var cloud in clouds)
        {
            if (!cloud.ContainsPositionWithRadius(position, radius))
                continue;

            cloud.ConvertToCloudLocal(position, out var cloudRelativeX, out var cloudRelativeY);

            int xEnd = (int)Mathf.Round(cloudRelativeX + localGrabRadius);
            int yEnd = (int)Mathf.Round(cloudRelativeY + localGrabRadius);

            for (int x = (int)Mathf.Round(cloudRelativeX - localGrabRadius); x <= xEnd; x += 1)
            {
                for (int y = (int)Mathf.Round(cloudRelativeY - localGrabRadius); y <= yEnd; y += 1)
                {
                    if (x < 0 || y < 0)
                        continue;

                    float distance = Mathf.Sqrt(Mathf.Pow(x - cloudRelativeX, 2) + Mathf.Pow(y - cloudRelativeY, 2));
                    if (distance > localGrabRadius)
                        continue;

                    float factor = 1.0f - (distance / localGrabRadius);

                    if (x < cloud.Size && y < cloud.Size)
                    {
                        cloud.AbsorbCompounds(x, y, storage, totals, delta * factor, rate * factor);
                    }
                }
            }
        }
    }

    public Vector3? FindCompoundNearPoint(Vector3 position, Compound compound, float searchRadius = 200,
        float minConcentration = 120)
    {
        if (searchRadius < 1)
            throw new ArgumentException("searchRadius must be >= 1");

        int resolution = Resolution;
        float localRadius = searchRadius / resolution;
        float nearestDistanceSquared = float.MaxValue;
        Vector3? closestPoint = null;

        foreach (var cloud in clouds)
        {
            if (!cloud.HandlesCompound(compound))
                continue;

            if (!cloud.ContainsPositionWithRadius(position, searchRadius))
                continue;

            cloud.ConvertToCloudLocal(position, out var cloudRelativeX, out var cloudRelativeY);

            for (int radius = 1; radius < localRadius; radius += 1)
            {
                for (double theta = 0; theta <= MathUtils.FULL_CIRCLE; theta += Constants.CHEMORECEPTOR_ARC_SIZE)
                {
                    int x = cloudRelativeX + (int)Math.Round(Math.Cos(theta) * radius);
                    int y = cloudRelativeY + (int)Math.Round(Math.Sin(theta) * radius);

                    if (x < 0 || y < 0)
                        continue;

                    if (x < cloud.Size && y < cloud.Size)
                    {
                        if (cloud.AmountAvailable(compound, x, y) >= minConcentration)
                        {
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

    public void EmptyAllClouds()
    {
        foreach (var cloud in clouds)
            cloud.ClearContents();
    }

    public void ReportPlayerPosition(Vector3 position)
    {
        var targetCenter = CalculateGridCenterForPlayerPos(position);

        if (!cloudGridCenter.Equals(targetCenter))
        {
            cloudGridCenter = targetCenter;
            PositionClouds();
        }
    }

    public void SetBrightnessModifier(float brightness)
    {
        if (Math.Abs(brightness - currentBrightness) < 0.001f)
            return;

        currentBrightness = brightness;

        foreach (var cloud in clouds)
        {
            cloud.SetBrightness(currentBrightness);
        }
    }

    private static Vector3 CalculateGridCenterForPlayerPos(Vector3 pos)
    {
        return new Vector3((int)Math.Round(pos.X / (Constants.CLOUD_X_EXTENT / 3)),
            0,
            (int)Math.Round(pos.Z / (Constants.CLOUD_Y_EXTENT / 3)));
    }

    private void PositionClouds()
    {
        foreach (var cloud in clouds)
        {
            cloud.Position = cloudGridCenter * Constants.CLOUD_Y_EXTENT / 3;
            cloud.UpdatePosition(new Vector2I((int)cloudGridCenter.X, (int)cloudGridCenter.Z));
        }
    }

    private void UpdateCloudContents(float delta)
    {
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

        executor.RunTasks(tasks);
        tasks.Clear();

        foreach (var cloud in clouds)
        {
            cloud.UpdateEdgesAfterCenter(delta);
        }

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
