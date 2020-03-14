using System;
using System.Collections.Generic;
using Godot;

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

    private int size;
    private int resolution;

    private Slot slot1;
    private Slot slot2;
    private Slot slot3;
    private Slot slot4;
    private Slot[] slots = new Slot[4];

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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        size = Settings.Instance.CloudSimulationWidth;
        resolution = Settings.Instance.CloudResolution;
        image = new Image();
        image.Create(size, size, false, Image.Format.Rgba8);
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
        slot1 = new Slot(cloud1, size, resolution, fluidSystem);
        slot2 = new Slot(cloud2, size, resolution, fluidSystem);
        slot3 = new Slot(cloud3, size, resolution, fluidSystem);
        slot4 = new Slot(cloud4, size, resolution, fluidSystem);

        slots[0] = slot1;
        slots[1] = slot2;
        slots[2] = slot3;
        slots[3] = slot4;

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

        slot1.Update(delta, pos);
        slot2.Update(delta, pos);
        slot3.Update(delta, pos);
        slot4.Update(delta, pos);
    }

    /// <summary>
    ///   Updates the edge concentrations of this cloud. This is not ran in parallel.
    /// </summary>
    public void UpdateEdges(float delta)
    {
        // TODO: implement
    }

    /// <summary>
    ///   Updates the texture with the new densities
    /// </summary>
    public void UploadTexture()
    {
        image.Lock();

        for (int x = 0; x < size; ++x)
        {
            for (int y = 0; y < size; ++y)
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

    /// <summary>
    ///   Adds some compound in cloud local coordinates
    /// </summary>
    public void AddCloud(Compound compound, float density, int x, int y)
    {
        GetSlot(compound).Density[x, y] += density;
    }

    /// <summary>
    ///   Takes some amount of compound, in cloud local coordinates.
    /// </summary>
    /// <returns>The amount of compound taken</returns>
    public float TakeCompound(Compound compound, int x, int y, float fraction = 1.0f)
    {
        var slot = GetSlot(compound);

        float amountToGive = slot.Density[x, y] * fraction;
        slot.Density[x, y] -= amountToGive;
        if (slot.Density[x, y] < 0.1f)
            slot.Density[x, y] = 0;

        return amountToGive;
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
        if (slot1.Compound != null)
        {
            float amount = slot1.Density[x, y];
            if (amount > 0)
                result[slot1.Compound.InternalName] = amount;
        }

        if (slot2.Compound != null)
        {
            float amount = slot2.Density[x, y];
            if (amount > 0)
                result[slot2.Compound.InternalName] = amount;
        }

        if (slot3.Compound != null)
        {
            float amount = slot3.Density[x, y];
            if (amount > 0)
                result[slot3.Compound.InternalName] = amount;
        }

        if (slot4.Compound != null)
        {
            float amount = slot4.Density[x, y];
            if (amount > 0)
                result[slot4.Compound.InternalName] = amount;
        }
    }

    /// <summary>
    ///   Checks if position is in this cloud, also returns relative coordinates
    /// </summary>
    public bool ContainsPosition(Vector3 worldPosition, out int x, out int y)
    {
        var topLeftRelative = worldPosition - Translation;

        // Floor is used here because otherwise the last coordinate is wrong
        x = (int)Math.Floor((topLeftRelative.x + Constants.CLOUD_WIDTH) / resolution);
        y = (int)Math.Floor((topLeftRelative.z + Constants.CLOUD_HEIGHT) / resolution);

        return x >= 0 && y >= 0 && x < Constants.CLOUD_WIDTH && y < Constants.CLOUD_HEIGHT;
    }

    /// <summary>
    ///   Returns true if position with radius around it contains any
    ///   points that are within this cloud.
    /// </summary>
    public bool CloudContainsPositionWithRadius(Vector3 worldPosition,
        float radius)
    {
        if (worldPosition.x + radius < Translation.x - Constants.CLOUD_WIDTH ||
            worldPosition.x - radius >= Translation.x + Constants.CLOUD_WIDTH ||
            worldPosition.z + radius < Translation.z - Constants.CLOUD_HEIGHT ||
            worldPosition.z - radius >= Translation.z + Constants.CLOUD_HEIGHT)
            return false;
        return true;
    }

    public void RecycleToPosition(Vector3 position)
    {
        Translation = position;

        ClearContents();
    }

    public void ClearContents()
    {
        if (slot1.Compound != null)
            slot1.Clear();

        if (slot2.Compound != null)
            slot2.Clear();

        if (slot3.Compound != null)
            slot3.Clear();

        if (slot4.Compound != null)
            slot4.Clear();
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

    private class Slot
    {
        public float[,] Density;
        public float[,] OldDensity;
        public Compound Compound;
        private readonly int size;
        private readonly int resolution;
        private readonly FluidSystem fluidSystem;

        public Slot(Compound compound, int size, int resolution, FluidSystem fluidSystem)
        {
            this.size = size;
            this.resolution = resolution;
            this.fluidSystem = fluidSystem;
            Compound = compound;

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
            Diffuse(0.007f, OldDensity, Density, delta);

            // Move the compound clouds about the velocity field.
            Advect(OldDensity, Density, delta, pos);
        }

        private void Diffuse(float diffRate, float[,] oldDens, float[,] density,
            float dt)
        {
            float a = dt * diffRate;
            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    oldDens[x, y] = (density[x, y] * (1 - a)) + ((oldDens[x - 1, y] +
                        oldDens[x + 1, y] + oldDens[x, y - 1] + oldDens[x, y + 1]) * a / 4);
                }
            }
        }

        private void Advect(float[,] oldDens, float[,] density, float dt, Vector2 pos)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    density[x, y] = 0;
                }
            }

            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    if (oldDens[x, y] > 1)
                    {
                        // TODO: give each cloud a viscosity value in the
                        // JSON file and use it instead.
                        const float viscosity =
                            0.0525f;
                        var velocity = fluidSystem.VelocityAt(
                            pos + (new Vector2(x, y) * resolution)) * viscosity;

                        float dx = x + (dt * velocity.x);
                        float dy = y + (dt * velocity.y);

                        dx = dx.Clamp(0.5f, size - 1.5f);
                        dy = dy.Clamp(0.5f, size - 1.5f);

                        int x0 = (int)dx;
                        int x1 = x0 + 1;
                        int y0 = (int)dy;
                        int y1 = y0 + 1;

                        float s1 = dx - x0;
                        float s0 = 1.0f - s1;
                        float t1 = dy - y0;
                        float t0 = 1.0f - t1;

                        density[x0, y0] += oldDens[x, y] * s0 * t0;
                        density[x0, y1] += oldDens[x, y] * s0 * t1;
                        density[x1, y0] += oldDens[x, y] * s1 * t0;
                        density[x1, y1] += oldDens[x, y] * s1 * t1;
                    }
                }
            }
        }
    }
}
