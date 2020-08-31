using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

public class CompoundCloudPlane : CSGMesh, ISaveApplyable
{
    /// <summary>
    ///   The current densities of compounds. This uses custom writing so this is ignored
    /// </summary>
    public Vector4[,] Density;

    [JsonIgnore]
    public Vector4[,] OldDensity;

    [JsonProperty]
    public Compound[] Compounds;

    // TODO: give each cloud a viscosity value in the
    // JSON file and use it instead.
    private const float VISCOSITY = 0.0525f;

    private Image image;
    private ImageTexture texture;
    private FluidSystem fluidSystem;

    [JsonProperty]
    private Int2 position = new Int2(0, 0);

    [JsonProperty]
    public int Resolution { get; private set; }

    [JsonProperty]
    public int Size { get; private set; }

    public bool IsLoadedFromSave { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (IsLoadedFromSave)
            return;

        Size = Settings.Instance.CloudSimulationWidth;
        Resolution = Settings.Instance.CloudResolution;
        CreateDensityTexture();

        Density = new Vector4[Size, Size];
        OldDensity = new Vector4[Size, Size];
        ClearContents();
    }

    public void UpdatePosition(Int2 newPosition)
    {
        // Whoever made the modulus operator return negatives: i hate u.
        int newx = ((newPosition.x % Constants.CLOUD_SQUARES_PER_SIDE) + Constants.CLOUD_SQUARES_PER_SIDE)
            % Constants.CLOUD_SQUARES_PER_SIDE;
        int newy = ((newPosition.y % Constants.CLOUD_SQUARES_PER_SIDE) + Constants.CLOUD_SQUARES_PER_SIDE)
            % Constants.CLOUD_SQUARES_PER_SIDE;

        if (newx == (position.x + 1) % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            PartialClearDensity(position.x * Size / Constants.CLOUD_SQUARES_PER_SIDE, 0,
                Size / Constants.CLOUD_SQUARES_PER_SIDE, Size);
        }
        else if (newx == (position.x + Constants.CLOUD_SQUARES_PER_SIDE - 1)
            % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            PartialClearDensity(((position.x + Constants.CLOUD_SQUARES_PER_SIDE - 1)
                    % Constants.CLOUD_SQUARES_PER_SIDE) * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                0, Size / Constants.CLOUD_SQUARES_PER_SIDE, Size);
        }

        if (newy == (position.y + 1) % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            PartialClearDensity(0, position.y * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                Size, Size / Constants.CLOUD_SQUARES_PER_SIDE);
        }
        else if (newy == (position.y + Constants.CLOUD_SQUARES_PER_SIDE - 1) % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            PartialClearDensity(0, ((position.y + Constants.CLOUD_SQUARES_PER_SIDE - 1)
                    % Constants.CLOUD_SQUARES_PER_SIDE) * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                Size, Size / Constants.CLOUD_SQUARES_PER_SIDE);
        }

        position = new Int2(newx, newy);

        // This accommodates the texture of the cloud to the new position of the plane.
        SetMaterialUVForPosition();
    }

    /// <summary>
    ///   Initializes this cloud. cloud2 onwards can be null
    /// </summary>
    public void Init(FluidSystem fluidSystem, Compound cloud1, Compound cloud2,
        Compound cloud3, Compound cloud4)
    {
        this.fluidSystem = fluidSystem;
        Compounds = new Compound[Constants.CLOUDS_IN_ONE] { cloud1, cloud2, cloud3, cloud4 };

        // Setup colours
        var material = (ShaderMaterial)Material;

        material.SetShaderParam("colour1", cloud1.Colour);

        var blank = new Color(0, 0, 0, 0);

        material.SetShaderParam("colour2", cloud2?.Colour ?? blank);
        material.SetShaderParam("colour3", cloud3?.Colour ?? blank);
        material.SetShaderParam("colour4", cloud4?.Colour ?? blank);
    }

    /// <summary>
    ///   Updates the edge concentrations of this cloud before the rest of the cloud.
    ///   This is not ran in parallel.
    /// </summary>
    public void UpdateEdgesBeforeCenter(float delta)
    {
        // The diffusion rate seems to have a bigger effect
        delta *= 100.0f;

        if (position.x != 0)
        {
            PartialDiffuseEdges(0, 0, Constants.CLOUD_EDGE_WIDTH / 2, Size, delta);
            PartialDiffuseEdges(Size - Constants.CLOUD_EDGE_WIDTH / 2, 0, Constants.CLOUD_EDGE_WIDTH / 2, Size, delta);
        }

        if (position.x != 1)
        {
            PartialDiffuseEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size, delta);
        }

        if (position.x != 2)
        {
            PartialDiffuseEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size, delta);
        }

        if (position.y != 0)
        {
            PartialDiffuseEdges(Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta);
            PartialDiffuseEdges(Constants.CLOUD_EDGE_WIDTH / 2, Size - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta);
            PartialDiffuseEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta);
            PartialDiffuseEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size - Constants.CLOUD_EDGE_WIDTH / 2, Size / Constants.CLOUD_SQUARES_PER_SIDE
                - Constants.CLOUD_EDGE_WIDTH, Constants.CLOUD_EDGE_WIDTH / 2, delta);
            PartialDiffuseEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                0, Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta);
            PartialDiffuseEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta);
        }

        if (position.y != 1)
        {
            PartialDiffuseEdges(Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta);
            PartialDiffuseEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta);
            PartialDiffuseEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta);
        }

        if (position.y != 2)
        {
            PartialDiffuseEdges(Constants.CLOUD_EDGE_WIDTH / 2,
                2 * Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta);
            PartialDiffuseEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Constants.CLOUD_EDGE_WIDTH * Size / Constants.CLOUD_SQUARES_PER_SIDE
                - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta);
            PartialDiffuseEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                2 * Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta);
        }
    }

    /// <summary>
    ///   Updates the edge concentrations of this cloud after the rest of the cloud.
    ///   This is not ran in parallel.
    /// </summary>
    public void UpdateEdgesAfterCenter(float delta)
    {
        // The diffusion rate seems to have a bigger effect
        delta *= 100.0f;
        var pos = new Vector2(Translation.x, Translation.z);

        if (position.x != 0)
        {
            PartialAdvectEdges(0, 0, Constants.CLOUD_EDGE_WIDTH / 2, Size, delta, pos);
            PartialAdvectEdges(Size - Constants.CLOUD_EDGE_WIDTH / 2, 0, Constants.CLOUD_EDGE_WIDTH / 2,
                Size, delta, pos);
        }

        if (position.x != 1)
        {
            PartialAdvectEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                0, Constants.CLOUD_EDGE_WIDTH, Size, delta, pos);
        }

        if (position.x != 2)
        {
            PartialAdvectEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                0, Constants.CLOUD_EDGE_WIDTH, Size, delta, pos);
        }

        if (position.y != 0)
        {
            PartialAdvectEdges(Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta, pos);
            PartialAdvectEdges(Constants.CLOUD_EDGE_WIDTH / 2, Size - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta, pos);
            PartialAdvectEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                0, Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta, pos);
            PartialAdvectEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta, pos);
            PartialAdvectEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                0, Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta, pos);
            PartialAdvectEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta, pos);
        }

        if (position.y != 1)
        {
            PartialAdvectEdges(Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta, pos);
            PartialAdvectEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta, pos);
            PartialAdvectEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta, pos);
        }

        if (position.y != 2)
        {
            PartialAdvectEdges(Constants.CLOUD_EDGE_WIDTH / 2,
                2 * Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta, pos);
            PartialAdvectEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Constants.CLOUD_EDGE_WIDTH * Size / Constants.CLOUD_SQUARES_PER_SIDE
                - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta, pos);
            PartialAdvectEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                2 * Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH, delta, pos);
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueUpdateCloud(float delta, List<Task> queue)
    {
        // The diffusion rate seems to have a bigger effect
        delta *= 100.0f;
        var pos = new Vector2(Translation.x, Translation.z);

        for (int i = 0; i < Constants.CLOUD_SQUARES_PER_SIDE; i++)
        {
            for (int j = 0; j < Constants.CLOUD_SQUARES_PER_SIDE; j++)
            {
                var x0 = i;
                var y0 = j;
                var task = new Task(() => PartialUpdateCenter(x0 * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                    y0 * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                    Size / Constants.CLOUD_SQUARES_PER_SIDE,
                    Size / Constants.CLOUD_SQUARES_PER_SIDE,
                    delta, pos));
                queue.Add(task);
            }
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueUpdateTextureImage(List<Task> queue)
    {
        image.Lock();

        for (int i = 0; i < Constants.CLOUD_SQUARES_PER_SIDE; i++)
        {
            for (int j = 0; j < Constants.CLOUD_SQUARES_PER_SIDE; j++)
            {
                var x0 = i;
                var y0 = j;
                var task = new Task(() => PartialUpdateTextureImage(x0 * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                    y0 * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                    Size / Constants.CLOUD_SQUARES_PER_SIDE,
                    Size / Constants.CLOUD_SQUARES_PER_SIDE));
                queue.Add(task);
            }
        }
    }

    public void UpdateTexture()
    {
        image.Unlock();
        texture.CreateFromImage(image, (uint)Texture.FlagsEnum.Filter | (uint)Texture.FlagsEnum.Repeat);
    }

    public bool HandlesCompound(Compound compound)
    {
        foreach (var c in Compounds)
        {
            if (c == compound)
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Adds some compound in cloud local coordinates
    /// </summary>
    public void AddCloud(Compound compound, float density, int x, int y)
    {
        var cloudToAdd = new Vector4(
            Compounds[0] == compound ? density : 0.0f,
            Compounds[1] == compound ? density : 0.0f,
            Compounds[2] == compound ? density : 0.0f,
            Compounds[3] == compound ? density : 0.0f);

        Density[x, y] += cloudToAdd;
    }

    /// <summary>
    ///   Takes some amount of compound, in cloud local coordinates.
    /// </summary>
    /// <returns>The amount of compound taken</returns>
    public float TakeCompound(Compound compound, int x, int y, float fraction = 1.0f)
    {
        float amountInCloud = HackyAdress(Density[x, y], GetCompoundIndex(compound));
        float amountToGive = amountInCloud * fraction;
        if (amountInCloud - amountToGive < 0.1f)
            AddCloud(compound, -amountInCloud, x, y);
        else
            AddCloud(compound, -amountToGive, x, y);

        return amountToGive;
    }

    /// <summary>
    ///   Calculates how much TakeCompound would take without actually taking the amount
    /// </summary>
    /// <returns>The amount available for taking</returns>
    public float AmountAvailable(Compound compound, int x, int y, float fraction = 1.0f)
    {
        float amountInCloud = HackyAdress(Density[x, y], GetCompoundIndex(compound));
        float amountToGive = amountInCloud * fraction;
        return amountToGive;
    }

    /// <summary>
    ///   Returns all the compounds that are available at point
    /// </summary>
    public void GetCompoundsAt(int x, int y, Dictionary<Compound, float> result)
    {
        for (int i = 0; i < Constants.CLOUDS_IN_ONE; i++)
        {
            if (Compounds[i] == null)
                break;

            float amount = HackyAdress(Density[x, y], i);
            if (amount > 0)
                result[Compounds[i]] = amount;
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
        x = ((int)Math.Floor((topLeftRelative.x + Constants.CLOUD_WIDTH) / Resolution)
            + position.x * Size / Constants.CLOUD_SQUARES_PER_SIDE) % Size;
        y = ((int)Math.Floor((topLeftRelative.z + Constants.CLOUD_HEIGHT) / Resolution)
            + position.y * Size / Constants.CLOUD_SQUARES_PER_SIDE) % Size;
    }

    /// <summary>
    ///   Absorbs compounds from this cloud
    /// </summary>
    public void AbsorbCompounds(int localX, int localY, CompoundBag storage,
        Dictionary<Compound, float> totals, float delta, float rate)
    {
        var fractionToTake = 1.0f - (float)Math.Pow(0.5f, delta / Constants.CLOUD_ABSORPTION_HALF_LIFE);

        for (int i = 0; i < Constants.CLOUDS_IN_ONE; i++)
        {
            if (Compounds[i] == null)
                break;

            // Overestimate of how much compounds we get
            float generousAmount = HackyAdress(Density[localX, localY], i) *
                Constants.SKIP_TRYING_TO_ABSORB_RATIO;

            // Skip if there isn't enough to absorb
            if (generousAmount < MathUtils.EPSILON)
                continue;

            float freeSpace = storage.Capacity - storage.GetCompoundAmount(Compounds[i]);

            float multiplier = 1.0f * rate;

            if (freeSpace < generousAmount)
            {
                // Allow partial absorption to allow cells to take from high density clouds
                multiplier = freeSpace / generousAmount;
            }

            float taken = TakeCompound(Compounds[i], localX, localY, fractionToTake * multiplier) *
                Constants.ABSORPTION_RATIO;

            storage.AddCompound(Compounds[i], taken);

            // Keep track of total compounds absorbed for the cell
            if (!totals.ContainsKey(Compounds[i]))
            {
                totals.Add(Compounds[i], taken);
            }
            else
            {
                totals[Compounds[i]] += taken;
            }
        }
    }

    public void ClearContents()
    {
        for (int x = 0; x < Size; ++x)
        {
            for (int y = 0; y < Size; ++y)
            {
                Density[x, y] = Vector4.Zero;
                OldDensity[x, y] = Vector4.Zero;
            }
        }
    }

    public void ApplyPropertiesFromSave(CompoundCloudPlane cloud)
    {
        Density = cloud.Density;

        // Re-init with potentially changed compounds
        Init(fluidSystem, cloud.Compounds[0], cloud.Compounds[1], cloud.Compounds[2], cloud.Compounds[3]);

        position = cloud.position;
        Transform = cloud.Transform;

        // Recreate the texture if the size changes
        // TODO: could resample the density data here to allow changing the cloud resolution or size
        // without starting a new save
        if (Size != cloud.Size || Resolution != cloud.Resolution)
        {
            Size = cloud.Size;
            Resolution = cloud.Resolution;
            CreateDensityTexture();
        }

        OldDensity = new Vector4[Size, Size];
        SetMaterialUVForPosition();
    }

    public void ApplySave(object loaded, ISaveContext context)
    {
        ApplyPropertiesFromSave((CompoundCloudPlane)loaded);
        IsLoadedFromSave = true;
    }

    /// <summary>
    ///   Calculates the multipliers for the old density to move to new locations
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The name might not be super accurate as I just picked something to reduce code duplication
    ///   </para>
    /// </remarks>
    private static void CalculateMovementFactors(float dx, float dy, out int q0, out int q1, out int r0, out int r1,
        out float s1, out float s0, out float t1, out float t0)
    {
        q0 = (int)Math.Floor(dx);
        q1 = q0 + 1;
        r0 = (int)Math.Floor(dy);
        r1 = r0 + 1;

        s1 = Math.Abs(dx - q0);
        s0 = 1.0f - s1;
        t1 = Math.Abs(dy - r0);
        t0 = 1.0f - t1;
    }

    private void PartialDiffuseCenter(int x0, int y0, int width, int height, float delta)
    {
        float a = delta * Constants.CLOUD_DIFFUSION_RATE;

        for (int x = x0; x < x0 + width; x++)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                int adjacentClouds = 4;
                OldDensity[x, y] =
                    Density[x, y] * (1 - a) +
                    (Density[x, y - 1] + Density[x, y + 1] + Density[x - 1, y] + Density[x + 1, y])
                    * (a / adjacentClouds);
            }
        }
    }

    private void PartialDiffuseEdges(int x0, int y0, int width, int height, float delta)
    {
        float a = delta * Constants.CLOUD_DIFFUSION_RATE;

        for (int x = x0; x < x0 + width; x++)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                int adjacentClouds = 4;
                OldDensity[x, y] =
                    Density[x, y] * (1 - a) +
                    (Density[x, (y - 1 + Size) % Size] +
                        Density[x, (y + 1) % Size] +
                        Density[(x - 1 + Size) % Size, y] +
                        Density[(x + 1) % Size, y]) * (a / adjacentClouds);
            }
        }
    }

    private void PartialAdvectCenter(int x0, int y0, int width, int height, float delta, Vector2 pos)
    {
        for (int x = x0; x < x0 + width; x++)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                if (OldDensity[x, y].LengthSquared() > 1)
                {
                    var velocity = fluidSystem.VelocityAt(
                        pos + (new Vector2(x, y) * Resolution)) * VISCOSITY;

                    // This is ran in parallel, this may not touch the other compound clouds
                    float dx = x + (delta * velocity.x);
                    float dy = y + (delta * velocity.y);

                    // So this is clamped to not go to the other clouds
                    dx = dx.Clamp(x0 - 0.5f, x0 + width + 0.5f);
                    dy = dy.Clamp(y0 - 0.5f, y0 + height + 0.5f);

                    CalculateMovementFactors(dx, dy, out var q0, out var q1, out var r0, out var r1,
                        out var s1, out var s0, out var t1, out var t0);

                    Density[q0, r0] += OldDensity[x, y] * s0 * t0;
                    Density[q0, r1] += OldDensity[x, y] * s0 * t1;
                    Density[q1, r0] += OldDensity[x, y] * s1 * t0;
                    Density[q1, r1] += OldDensity[x, y] * s1 * t1;
                }
            }
        }
    }

    private void PartialAdvectEdges(int x0, int y0, int width, int height, float delta, Vector2 pos)
    {
        for (int x = x0; x < x0 + width; x++)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                if (OldDensity[x, y].LengthSquared() > 1)
                {
                    var velocity = fluidSystem.VelocityAt(
                        pos + (new Vector2(x, y) * Resolution)) * VISCOSITY;

                    // This is ran in parallel, this may not touch the other compound clouds
                    float dx = x + (delta * velocity.x);
                    float dy = y + (delta * velocity.y);

                    CalculateMovementFactors(dx, dy, out var q0, out var q1, out var r0, out var r1,
                        out var s1, out var s0, out var t1, out var t0);

                    Density[(q0 + Size) % Size, (r0 + Size) % Size] += OldDensity[x, y] * s0 * t0;
                    Density[(q0 + Size) % Size, (r1 + Size) % Size] += OldDensity[x, y] * s0 * t1;
                    Density[(q1 + Size) % Size, (r0 + Size) % Size] += OldDensity[x, y] * s1 * t0;
                    Density[(q1 + Size) % Size, (r1 + Size) % Size] += OldDensity[x, y] * s1 * t1;
                }
            }
        }
    }

    private void PartialUpdateTextureImage(int x0, int y0, int width, int height)
    {
        for (int x = x0; x < x0 + width; x++)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                var pixel = Density[x, y] * (1 / Constants.CLOUD_MAX_INTENSITY_SHOWN);
                image.SetPixel(x, y, new Color(pixel.X, pixel.Y, pixel.Z, pixel.W));
            }
        }
    }

    private void PartialClearDensity(int x0, int y0, int width, int height)
    {
        for (int x = x0; x < x0 + width; x++)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                Density[x, y] = Vector4.Zero;
            }
        }
    }

    private void PartialUpdateCenter(int x0, int y0, int width, int height, float delta, Vector2 pos)
    {
        PartialDiffuseCenter(x0 + Constants.CLOUD_EDGE_WIDTH / 2, y0 + Constants.CLOUD_EDGE_WIDTH / 2, width
            - Constants.CLOUD_EDGE_WIDTH, height - Constants.CLOUD_EDGE_WIDTH, delta);
        PartialClearDensity(x0, y0, width, height);
        PartialAdvectCenter(x0 + Constants.CLOUD_EDGE_WIDTH / 2, y0 + Constants.CLOUD_EDGE_WIDTH / 2, width
            - Constants.CLOUD_EDGE_WIDTH, height - Constants.CLOUD_EDGE_WIDTH, delta, pos);
    }

    private float HackyAdress(Vector4 vector, int index)
    {
        switch (index)
        {
            case 0: return vector.X;
            case 1: return vector.Y;
            case 2: return vector.Z;
            case 3: return vector.W;
        }

        return 0;
    }

    private int GetCompoundIndex(Compound compound)
    {
        for (int i = 0; i < Constants.CLOUDS_IN_ONE; i++)
        {
            if (Compounds[i] == compound)
                return i;
        }

        return -1;
    }

    private void CreateDensityTexture()
    {
        image = new Image();
        image.Create(Size, Size, false, Image.Format.Rgba8);
        texture = new ImageTexture();
        texture.CreateFromImage(image, (uint)Texture.FlagsEnum.Filter | (uint)Texture.FlagsEnum.Repeat);

        var material = (ShaderMaterial)Material;
        material.SetShaderParam("densities", texture);
    }

    private void SetMaterialUVForPosition()
    {
        var material = (ShaderMaterial)Material;
        material.SetShaderParam("UVoffset", new Vector2(position.x / (float)Constants.CLOUD_SQUARES_PER_SIDE,
            position.y / (float)Constants.CLOUD_SQUARES_PER_SIDE));
    }
}
