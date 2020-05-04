using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class CompoundCloudPlane : CSGMesh
{
    // The 3x3 grid of density tiles around this cloud for moving compounds
    // between them
    // TODO: This isn't implemented
    public CompoundCloudPlane LeftCloud;
    public CompoundCloudPlane RightCloud;
    public CompoundCloudPlane LowerCloud;
    public CompoundCloudPlane UpperCloud;

    private Image image;
    private ImageTexture texture;

    private Slot slot1;
    private Slot slot2;
    private Slot slot3;
    private Slot slot4;
    private List<Slot> slots;

    public Compound Compound1
    {
        get
        {
            return slot1.Compound;
        }
    }

    public Compound Compound2
    {
        get
        {
            return slot2.Compound;
        }
    }

    public Compound Compound3
    {
        get
        {
            return slot3.Compound;
        }
    }

    public Compound Compound4
    {
        get
        {
            return slot4.Compound;
        }
    }

    [JsonProperty]
    public int Resolution { get; private set; }

    [JsonProperty]
    public int Size { get; private set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Size = Settings.Instance.CloudSimulationWidth;
        Resolution = Settings.Instance.CloudResolution;
        image = new Image();
        image.Create(Size, Size, false, Image.Format.Rgba8);
        texture = new ImageTexture();
        texture.CreateFromImage(image, (uint)Texture.FlagsEnum.Filter);

        var material = (ShaderMaterial)this.Material;
        material.SetShaderParam("densities", texture);
    }

    /// <summary>
    ///   Initializes this cloud. cloud2 onwards can be null
    /// </summary>
    public void Init(FluidSystem fluidSystem, Compound cloud1, Compound cloud2,
        Compound cloud3, Compound cloud4)
    {
        // Setup slots
        slot1 = new Slot(cloud1, Size, Resolution, fluidSystem, this, 0);
        slot2 = new Slot(cloud2, Size, Resolution, fluidSystem, this, 1);
        slot3 = new Slot(cloud3, Size, Resolution, fluidSystem, this, 2);
        slot4 = new Slot(cloud4, Size, Resolution, fluidSystem, this, 3);

        // These slots are used in many places where it probably helps
        // to not have null checks so this is dynamically sized. But
        // all of the slots always exist to make the texture upload
        // tight loop not have if conditions
        slots = new List<Slot>();

        slots.Add(slot1);

        if (cloud2 != null)
            slots.Add(slot2);

        if (cloud3 != null)
            slots.Add(slot3);

        if (cloud4 != null)
            slots.Add(slot4);

        // Setup colours
        var material = (ShaderMaterial)this.Material;

        material.SetShaderParam("colour1", cloud1.Colour);

        var blank = new Color(0, 0, 0, 0);

        material.SetShaderParam("colour2", cloud2 != null ? cloud2.Colour : blank);
        material.SetShaderParam("colour3", cloud3 != null ? cloud3.Colour : blank);
        material.SetShaderParam("colour4", cloud4 != null ? cloud4.Colour : blank);
    }

    /// <summary>
    ///   Applies diffuse and advect for this single cloud. This is
    ///   ran in parallel for all clouds.
    /// </summary>
    public void UpdateCloud(float delta)
    {
        // The diffusion rate seems to have a bigger effect
        delta *= 100.0f;
        var pos = new Vector2(Translation.x, Translation.z);
        foreach (var slot in slots)
            slot.Update(delta, pos);
    }

    /// <summary>
    ///   Updates the edge concentrations of this cloud before the rest of the cloud.
    ///   This is not ran in parallel.
    /// </summary>
    public void UpdateEdgesBeforeCenter(float delta)
    {
        delta *= 100.0f;

        foreach (var slot in slots)
        {
            // The top edge.
            for (int x = 0; x < Size; x++)
            {
                int y = 0;
                slot.DiffuseTile(x, y, delta);
            }

            // The bottom edge.
            for (int x = 0; x < Size; x++)
            {
                int y = Size - 1;
                slot.DiffuseTile(x, y, delta);
            }

            // The left edge.
            for (int y = 1; y < Size - 1; y++)
            {
                int x = 0;
                slot.DiffuseTile(x, y, delta);
            }

            // The right edge.
            for (int y = 1; y < Size - 1; y++)
            {
                int x = Size - 1;
                slot.DiffuseTile(x, y, delta);
            }
        }
    }

    /// <summary>
    ///   Updates the edge concentrations of this cloud after the rest of the cloud.
    ///   This is not ran in parallel.
    /// </summary>
    public void UpdateEdgesAfterCenter(float delta)
    {
        delta *= 100.0f;
        var pos = new Vector2(Translation.x, Translation.z);

        foreach (var slot in slots)
        {
            // The top edge.
            for (int x = 0; x < Size; x++)
            {
                int y = 0;
                slot.AdvectTile(x, y, delta, pos);
            }

            // The bottom edge.
            for (int x = 0; x < Size; x++)
            {
                int y = Size - 1;
                slot.AdvectTile(x, y, delta, pos);
            }

            // The left edge.
            for (int y = 1; y < Size - 1; y++)
            {
                int x = 0;
                slot.AdvectTile(x, y, delta, pos);
            }

            // The right edge.
            for (int y = 1; y < Size - 1; y++)
            {
                int x = Size - 1;
                slot.AdvectTile(x, y, delta, pos);
            }
        }
    }

    /// <summary>
    ///   Updates the texture with the new densities
    /// </summary>
    public void UploadTexture()
    {
        image.Lock();

        for (int x = 0; x < Size; ++x)
        {
            for (int y = 0; y < Size; ++y)
            {
                // This formula smoothens the cloud density so that we get gradients
                // of transparency.
                float intensity1 = 2 * Mathf.Atan(
                        0.003f * slot1.Density[x, y]);
                float intensity2 = 2 * Mathf.Atan(
                        0.003f * slot2.Density[x, y]);
                float intensity3 = 2 * Mathf.Atan(
                        0.003f * slot3.Density[x, y]);
                float intensity4 = 2 * Mathf.Atan(
                        0.003f * slot4.Density[x, y]);

                // There used to be a clamp(0.0f, 1.0f) for all the
                // values but that has been taken out to improve
                // performance as with Godot it doesn't seem to have
                // much effect

                image.SetPixel(x, y,
                    new Color(intensity1, intensity2, intensity3, intensity4));
            }
        }

        image.Unlock();

        texture.CreateFromImage(image, (uint)Texture.FlagsEnum.Filter);
    }

    public bool HandlesCompound(Compound compound)
    {
        foreach (var slot in slots)
        {
            if (slot.Compound == compound)
                return true;
        }

        return false;
    }

    public bool HandlesCompound(string compound)
    {
        foreach (var slot in slots)
        {
            if (slot.Compound.InternalName == compound)
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Adds some compound in cloud local coordinates
    /// </summary>
    public void AddCloud(Compound compound, float density, int x, int y)
    {
        GetSlot(compound).Density[x, y] += density;
    }

    public void AddCloud(string compound, float density, int x, int y)
    {
        GetSlot(compound).Density[x, y] += density;
    }

    /// <summary>
    ///   Takes some amount of compound, in cloud local coordinates.
    /// </summary>
    /// <returns>The amount of compound taken</returns>
    public float TakeCompound(Compound compound, int x, int y, float fraction = 1.0f)
    {
        return GetSlot(compound).TakeCompound(x, y, fraction);
    }

    /// <summary>
    ///   Calculates how much TakeCompound would take without actually taking the amount
    /// </summary>
    /// <returns>The amount available for taking</returns>
    public float AmountAvailable(Compound compound, int x, int y, float fraction = 1.0f)
    {
        var slot = GetSlot(compound);

        float amountToGive = slot.Density[x, y] * fraction;
        return amountToGive;
    }

    /// <summary>
    ///   Returns all the compounds that are available at point
    /// </summary>
    public void GetCompoundsAt(int x, int y, Dictionary<string, float> result)
    {
        foreach (var slot in slots)
        {
            float amount = slot.Density[x, y];
            if (amount > 0)
                result[slot.Compound.InternalName] = amount;
        }
    }

    /// <summary>
    ///   Checks if position is in this cloud, also returns relative coordinates
    /// </summary>
    public bool ContainsPosition(Vector3 worldPosition, out int x, out int y)
    {
        ConvertToCloudLocal(worldPosition, out x, out y);

        return x >= 0 && y >= 0 && x < Constants.CLOUD_WIDTH && y < Constants.CLOUD_HEIGHT;
    }

    /// <summary>
    ///   Returns true if position with radius around it contains any
    ///   points that are within this cloud.
    /// </summary>
    public bool ContainsPositionWithRadius(Vector3 worldPosition,
        float radius)
    {
        if (worldPosition.x + radius < Translation.x - Constants.CLOUD_WIDTH ||
            worldPosition.x - radius >= Translation.x + Constants.CLOUD_WIDTH ||
            worldPosition.z + radius < Translation.z - Constants.CLOUD_HEIGHT ||
            worldPosition.z - radius >= Translation.z + Constants.CLOUD_HEIGHT)
            return false;
        return true;
    }

    /// <summary>
    ///   Converts world coordinate to cloud relative (top left) coordinates
    /// </summary>
    public void ConvertToCloudLocal(Vector3 worldPosition, out int x, out int y)
    {
        var topLeftRelative = worldPosition - Translation;

        // Floor is used here because otherwise the last coordinate is wrong
        x = (int)Math.Floor((topLeftRelative.x + Constants.CLOUD_WIDTH) / Resolution);
        y = (int)Math.Floor((topLeftRelative.z + Constants.CLOUD_HEIGHT) / Resolution);
    }

    /// <summary>
    ///   Absorbs compounds from this cloud
    /// </summary>
    public void AbsorbCompounds(int localX, int localY, CompoundBag storage,
        Dictionary<string, float> totals, float delta)
    {
        var fractionToTake = 1.0f - (float)Math.Pow(0.5f, delta / Constants.CLOUD_ABSORPTION_HALF_LIFE);

        foreach (var slot in slots)
        {
            // Overestimate of how much compounds we get
            float generousAmount = slot.Density[localX, localY] *
                Constants.SKIP_TRYING_TO_ABSORB_RATIO;

            // Skip if there isn't enough to absorb
            if (generousAmount < MathUtils.EPSILON)
                continue;

            var compound = slot.Compound.InternalName;

            float freeSpace = storage.Capacity - storage.GetCompoundAmount(compound);

            float multiplier = 1.0f;

            if (freeSpace < generousAmount)
            {
                // Allow partial absorption to allow cells to take from high density clouds
                multiplier = freeSpace / generousAmount;
            }

            float taken = slot.TakeCompound(localX, localY, fractionToTake * multiplier) *
                Constants.ABSORPTION_RATIO;

            storage.AddCompound(compound, taken);

            // Keep track of total compounds absorbed for the cell
            if (!totals.ContainsKey(compound))
            {
                totals.Add(compound, taken);
            }
            else
            {
                totals[compound] += taken;
            }
        }
    }

    public void RecycleToPosition(Vector3 position)
    {
        Translation = position;
        ClearContents();
    }

    public void ClearContents()
    {
        foreach (var slot in slots)
            slot.Clear();
    }

    private Slot GetSlot(Compound compound)
    {
        foreach (var slot in slots)
        {
            if (slot.Compound == compound)
                return slot;
        }

        throw new ArgumentException("compound not handled by this cloud", nameof(compound));
    }

    private Slot GetSlot(string compound)
    {
        foreach (var slot in slots)
        {
            if (slot.Compound.InternalName == compound)
                return slot;
        }

        throw new ArgumentException("compound not handled by this cloud", nameof(compound));
    }

    private void AddCloudDensity(int slotIndex, int x, int y, float value)
    {
        var xComponent = this;
        if (x < 0)
        {
            xComponent = LeftCloud;
        }
        else if (x >= Size)
        {
            xComponent = RightCloud;
        }

        if (xComponent == null)
            return;

        var yComponent = xComponent;
        if (y < 0)
        {
            yComponent = xComponent.UpperCloud;
        }
        else if (y >= Size)
        {
            yComponent = xComponent.LowerCloud;
        }

        if (yComponent == null)
            return;

        x = (x + Size) % Size;
        y = (y + Size) % Size;

        yComponent.slots[slotIndex].Density[x, y] += value;
    }

    private class Slot
    {
        public float[,] Density;
        public float[,] OldDensity;
        public Compound Compound;
        private readonly int size;
        private readonly int resolution;
        private readonly FluidSystem fluidSystem;
        private readonly CompoundCloudPlane parentPlane;
        private readonly int index;

        public Slot(Compound compound, int size, int resolution,
            FluidSystem fluidSystem, CompoundCloudPlane parent, int index)
        {
            this.size = size;
            this.resolution = resolution;
            this.fluidSystem = fluidSystem;
            Compound = compound;
            parentPlane = parent;
            this.index = index;

            if (size <= 0)
                return;

            // For simplicity all the densities always exist, even on the unused clouds
            Density = new float[size, size];

            // Except the old density can be easily ignored so this saves some memory
            if (Compound != null)
                OldDensity = new float[size, size];
        }

        public void Clear()
        {
            for (int x = 0; x < size; ++x)
            {
                for (int y = 0; y < size; ++y)
                {
                    Density[x, y] = 0.0f;
                }
            }
        }

        public void Update(float delta, Vector2 pos)
        {
            if (Compound == null)
                return;

            // Compound clouds move from area of high concentration to area of low.
            Diffuse(delta);
            ClearDensity();

            // Move the compound clouds about the velocity field.
            Advect(delta, pos);
        }

        public float TakeCompound(int x, int y, float fraction)
        {
            float amountToGive = Density[x, y] * fraction;
            Density[x, y] -= amountToGive;
            if (Density[x, y] < 0.1f)
                Density[x, y] = 0;

            return amountToGive;
        }

        public void DiffuseTile(int x, int y, float delta)
        {
            float a = delta * Constants.CLOUD_DIFFUSION_RATE;

            float upperDensity = 0.0f;
            if (y > 0)
            {
                upperDensity = Density[x, y - 1];
            }
            else if (parentPlane.UpperCloud != null)
            {
                upperDensity = parentPlane.UpperCloud.slots[index]
                                .Density[x, size - 1];
            }

            float lowerDensity = 0.0f;
            if (y < size - 1)
            {
                lowerDensity = Density[x, y + 1];
            }
            else if (parentPlane.LowerCloud != null)
            {
                lowerDensity =
                    parentPlane.LowerCloud.slots[index]
                                .Density[x, 0];
            }

            float leftDensity = 0.0f;
            if (x > 0)
            {
                leftDensity = Density[x - 1, y];
            }
            else if (parentPlane.LeftCloud != null)
            {
                leftDensity = parentPlane.LeftCloud.slots[index]
                                .Density[size - 1, y];
            }

            float rightDensity = 0.0f;
            if (x < size - 1)
            {
                rightDensity = Density[x + 1, y];
            }
            else if (parentPlane.RightCloud != null)
            {
                rightDensity =
                    parentPlane.RightCloud.slots[index]
                                .Density[0, y];
            }

            OldDensity[x, y] =
                Density[x, y] * (1 - a) +
                (upperDensity + lowerDensity + leftDensity + rightDensity) * a /
                    4;
        }

        public void AdvectTile(int x, int y, float delta, Vector2 pos)
        {
            if (OldDensity[x, y] > 1)
            {
                // TODO: give each cloud a viscosity value in the
                // JSON file and use it instead.
                const float viscosity =
                    0.0525f;
                var velocity = fluidSystem.VelocityAt(
                    pos + (new Vector2(x, y) * resolution)) * viscosity;

                float dx = x + delta * velocity.x;
                float dy = y + delta * velocity.x;

                int x0 = (int)Math.Floor(dx);
                int x1 = x0 + 1;
                int y0 = (int)Math.Floor(dy);
                int y1 = y0 + 1;

                float s1 = Math.Abs(dx - x0);
                float s0 = 1.0f - s1;
                float t1 = Math.Abs(dy - y0);
                float t0 = 1.0f - t1;

                parentPlane.AddCloudDensity(index, x0, y0,
                    OldDensity[x, y] * s0 * t0);
                parentPlane.AddCloudDensity(index, x0, y1,
                    OldDensity[x, y] * s0 * t1);
                parentPlane.AddCloudDensity(index, x1, y0,
                    OldDensity[x, y] * s1 * t0);
                parentPlane.AddCloudDensity(index, x1, y1,
                    OldDensity[x, y] * s1 * t1);
            }
        }

        private void ClearDensity()
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Density[x, y] = 0;
                }
            }
        }

        private void Diffuse(float delta)
        {
            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    DiffuseTile(x, y, delta);
                }
            }
        }

        private void Advect(float delta, Vector2 pos)
        {
            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    AdvectTile(x, y, delta, pos);
                }
            }
        }
    }
}
