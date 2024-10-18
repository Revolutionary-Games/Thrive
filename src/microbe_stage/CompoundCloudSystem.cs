using System;
using System.Collections.Generic;
using Godot;
using Systems;

[GodotAutoload]
public partial class CompoundCloudSystem : Node, IReadonlyCompoundClouds, ISaveLoadedTracked
{
    private List<Node> cloudInstances = new();
    private Vector3 cloudGridCenter;
    private double elapsed;
    private float currentBrightness = 1.0f;
    private PackedScene cloudScene = null!;
    private FluidCurrentsSystem? fluidCurrentsSystem;
    private bool isExiting = false;

    public bool IsLoadedFromSave { get; set; }

    public int NeededCloudsAtOnePosition { get; private set; }

    public override void _Ready()
    {
        base._Ready();

        if (Engine.IsEditorHint())
            return;

        cloudScene = GD.Load<PackedScene>("res://src/microbe_stage/CompoundCloudPlane.tscn");

        if (cloudScene == null)
        {
            GD.PrintErr("Failed to load CompoundCloudPlane scene.");
            return;
        }
    }

    public override void _ExitTree()
    {
        isExiting = true;
        base._ExitTree();

        foreach (var cloudInstance in cloudInstances)
        {
            cloudInstance.QueueFree();
        }
        cloudInstances.Clear();
    }

    public void Init(FluidCurrentsSystem fluidCurrentsSystem)
    {
        try
        {
            this.fluidCurrentsSystem = fluidCurrentsSystem;

            var allCloudCompounds = SimulationParameters.Instance.GetCloudCompounds();

            cloudInstances.Clear();

            NeededCloudsAtOnePosition = (int)Math.Ceiling(allCloudCompounds.Count / (float)Constants.CLOUDS_IN_ONE);

            for (int i = 0; i < NeededCloudsAtOnePosition; i++)
            {
                var cloudNode = cloudScene.Instantiate<Node>();

                if (cloudNode == null)
                {
                    GD.PrintErr($"Failed to create CompoundCloudPlane instance {i}");
                    return;
                }

                AddChild(cloudNode);
                cloudInstances.Add(cloudNode);

                int startOffset = i * Constants.CLOUDS_IN_ONE;
                int cloud1 = (startOffset < allCloudCompounds.Count) ? GetCompoundID(allCloudCompounds[startOffset]) : -1;
                int cloud2 = (startOffset + 1 < allCloudCompounds.Count) ? GetCompoundID(allCloudCompounds[startOffset + 1]) : -1;
                int cloud3 = (startOffset + 2 < allCloudCompounds.Count) ? GetCompoundID(allCloudCompounds[startOffset + 2]) : -1;
                int cloud4 = (startOffset + 3 < allCloudCompounds.Count) ? GetCompoundID(allCloudCompounds[startOffset + 3]) : -1;

                cloudNode.Call("init", -i - 1, cloud1, cloud2, cloud3, cloud4);
                cloudNode.Set("position", Vector3.Zero);
            }

            GD.Print("CompoundCloudSystem initialization completed successfully");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error in CompoundCloudSystem.Init: {e.Message}\n{e.StackTrace}");
        }
    }

    public override void _Process(double delta)
    {
        if (isExiting)
            return;

        try
        {
            elapsed += delta;

            if (elapsed >= Settings.Instance.CloudUpdateInterval)
            {
                elapsed = 0;
                foreach (var cloudInstance in cloudInstances)
                {
                    cloudInstance.Call("update_texture");
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error in CompoundCloudSystem._Process: {e.Message}\n{e.StackTrace}");
        }
    }

    private int GetCompoundID(CompoundDefinition compoundDefinition)
    {
        return (int)compoundDefinition.ID;
    }

    private int GetCompoundID(Compound compound)
    {
        return (int)compound;
    }

    public void SpawnCloud(Compound compound, float density, Vector3 worldPosition)
    {
        AddCloud(compound, density, worldPosition);
    }

    public bool AddCloud(Compound compound, float density, Vector3 worldPosition)
    {
        int compoundID = GetCompoundID(compound);

        foreach (var cloudInstance in cloudInstances)
        {
            bool contains = (bool)cloudInstance.Call("contains_position", worldPosition);
            if (contains)
            {
                var coords = (Vector2)cloudInstance.Call("convert_to_cloud_local", worldPosition);
                cloudInstance.Call("add_cloud", compoundID, (int)coords.X, (int)coords.Y, density);
                return true;
            }
        }

        return false;
    }

    public float TakeCompound(Compound compound, Vector3 worldPosition, float fraction)
    {
        if (fraction < 0.0f)
            throw new ArgumentException("Fraction to take can't be negative");

        int compoundID = GetCompoundID(compound);

        foreach (var cloudInstance in cloudInstances)
        {
            bool contains = (bool)cloudInstance.Call("contains_position", worldPosition);
            if (contains)
            {
                var coords = (Vector2)cloudInstance.Call("convert_to_cloud_local", worldPosition);
                return (float)cloudInstance.Call("take_compound", compoundID, (int)coords.X, (int)coords.Y, fraction);
            }
        }

        return 0;
    }

    public float AmountAvailable(Compound compound, Vector3 worldPosition, float fraction = 1.0f)
    {
        int compoundID = GetCompoundID(compound);

        foreach (var cloudInstance in cloudInstances)
        {
            bool contains = (bool)cloudInstance.Call("contains_position", worldPosition);
            if (contains)
            {
                var coords = (Vector2)cloudInstance.Call("convert_to_cloud_local", worldPosition);
                return (float)cloudInstance.Call("amount_available", compoundID, (int)coords.X, (int)coords.Y, fraction);
            }
        }

        return 0;
    }

    public void GetAllAvailableAt(Vector3 worldPosition, Dictionary<Compound, float> result, bool onlyAbsorbable = true)
    {
        foreach (var cloudInstance in cloudInstances)
        {
            bool contains = (bool)cloudInstance.Call("contains_position", worldPosition);
            if (contains)
            {
                var coords = (Vector2)cloudInstance.Call("convert_to_cloud_local", worldPosition);
                int x = (int)coords.X;
                int y = (int)coords.Y;

                var compounds = (int[])cloudInstance.Call("get_compounds");

                foreach (int compoundID in compounds)
                {
                    if (compoundID == -1)
                        continue;

                    float amount = (float)cloudInstance.Call("amount_available", compoundID, x, y, 1.0f);
                    var compound = (Compound)compoundID;

                    if (!result.ContainsKey(compound))
                    {
                        result[compound] = 0;
                    }

                    result[compound] += amount;
                }
            }
        }
    }

    public void EmptyAllClouds()
    {
        foreach (var cloudInstance in cloudInstances)
        {
            cloudInstance.Call("clear_contents");
        }
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

        foreach (var cloudInstance in cloudInstances)
        {
            cloudInstance.Call("set_brightness", currentBrightness);
        }
    }

    private static Vector3 CalculateGridCenterForPlayerPos(Vector3 pos)
    {
        return new Vector3(
            (int)Math.Round(pos.X / (Constants.CLOUD_X_EXTENT / 3)), 0,
            (int)Math.Round(pos.Z / (Constants.CLOUD_Y_EXTENT / 3)));
    }

    private void PositionClouds()
    {
        foreach (var cloudInstance in cloudInstances)
        {
            var newPosition = cloudGridCenter * Constants.CLOUD_Y_EXTENT / 3;
            cloudInstance.Set("position", newPosition);
            cloudInstance.Call("update_position", new Vector2(cloudGridCenter.X, cloudGridCenter.Z));
        }
    }

    public int Resolution
    {
        get
        {
            if (cloudInstances.Count > 0)
            {
                return (int)cloudInstances[0].Get("resolution");
            }

            return 4; // Default value
        }
    }

    public int Size
    {
        get
        {
            if (cloudInstances.Count > 0)
            {
                return (int)cloudInstances[0].Get("size");
            }

            return 256; // Default value
        }
    }

    public Vector3? FindCompoundNearPoint(Vector3 position, Compound compound, float searchRadius = 200, float minConcentration = 120)
    {
        if (searchRadius < 1)
            throw new ArgumentException("searchRadius must be >= 1");

        int resolution = Resolution;
        float localRadius = searchRadius / resolution;
        float localRadiusSquared = localRadius * localRadius;
        float nearestDistanceSquared = float.MaxValue;
        Vector3? closestPoint = null;
        int compoundID = GetCompoundID(compound);

        foreach (var cloudInstance in cloudInstances)
        {
            var compounds = (int[])cloudInstance.Call("get_compounds");

            if (!Array.Exists(compounds, id => id == compoundID))
                continue;

            bool contains = (bool)cloudInstance.Call("contains_position_with_radius", position, searchRadius);
            if (!contains)
                continue;

            var coords = (Vector2)cloudInstance.Call("convert_to_cloud_local", position);
            int cloudRelativeX = (int)coords.X;
            int cloudRelativeY = (int)coords.Y;

            int cloudSize = Size;

            int xStart = Math.Max(0, cloudRelativeX - (int)localRadius);
            int xEnd = Math.Min(cloudSize - 1, cloudRelativeX + (int)localRadius);
            int yStart = Math.Max(0, cloudRelativeY - (int)localRadius);
            int yEnd = Math.Min(cloudSize - 1, cloudRelativeY + (int)localRadius);

            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    float dx = x - cloudRelativeX;
                    float dy = y - cloudRelativeY;
                    float distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared > localRadiusSquared)
                        continue;

                    float amount = (float)cloudInstance.Call("amount_available", compoundID, x, y, 1.0f);

                    if (amount >= minConcentration)
                    {
                        Vector3 currentWorldPos = (Vector3)cloudInstance.Call("convert_to_world", x, y);
                        float worldDistanceSquared = (position - currentWorldPos).LengthSquared();

                        if (worldDistanceSquared < nearestDistanceSquared)
                        {
                            closestPoint = currentWorldPos;
                            nearestDistanceSquared = worldDistanceSquared;
                        }
                    }
                }
            }
        }

        return closestPoint;
    }

    public void AbsorbCompounds(Vector3 position, float radius, CompoundBag absorbBag, Dictionary<Compound, float> absorbTracker, float deltaTime, float absorbRate)
    {
        if (absorbRate <= 0.0f)
            throw new ArgumentException("Absorb rate must be positive");

        if (radius <= 0.0f)
            throw new ArgumentException("Radius must be positive");

        foreach (var cloudInstance in cloudInstances)
        {
            bool contains = (bool)cloudInstance.Call("contains_position_with_radius", position, radius);
            if (!contains)
                continue;

            var coords = (Vector2)cloudInstance.Call("convert_to_cloud_local", position);
            int cloudRelativeX = (int)coords.X;
            int cloudRelativeY = (int)coords.Y;

            int cloudSize = Size;
            int resolution = Resolution;

            int localRadius = (int)(radius / resolution) + 1;

            int xStart = Math.Max(0, cloudRelativeX - localRadius);
            int xEnd = Math.Min(cloudSize - 1, cloudRelativeX + localRadius);
            int yStart = Math.Max(0, cloudRelativeY - localRadius);
            int yEnd = Math.Min(cloudSize - 1, cloudRelativeY + localRadius);

            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    float dx = (x - cloudRelativeX) * resolution;
                    float dy = (y - cloudRelativeY) * resolution;
                    float distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared > radius * radius)
                        continue;

                    var compounds = (int[])cloudInstance.Call("get_compounds");

                    foreach (int compoundID in compounds)
                    {
                        if (compoundID == -1)
                            continue;

                        var compound = (Compound)compoundID;

                        if (!absorbBag.IsUseful(compound))
                            continue;

                        float availableAmount = (float)cloudInstance.Call("amount_available", compoundID, x, y, 1.0f);

                        if (availableAmount <= 0)
                            continue;

                        float fractionToAbsorb = absorbRate * deltaTime;

                        // Calculate the amount to absorb
                        float amountToAbsorb = availableAmount * fractionToAbsorb;

                        // Update the absorb bag and tracker
                        absorbBag.AddCompound(compound, amountToAbsorb);
                        if (absorbTracker.ContainsKey(compound))
                        {
                            absorbTracker[compound] += amountToAbsorb;
                        }
                        else
                        {
                            absorbTracker[compound] = amountToAbsorb;
                        }

                        // Remove the absorbed amount from the cloud
                        cloudInstance.Call("take_compound", compoundID, x, y, fractionToAbsorb);
                    }
                }
            }
        }
    }
}
