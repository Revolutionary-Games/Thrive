using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using Systems;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;
using Vector4 = System.Numerics.Vector4;

/// <summary>
///   A single compound cloud plane that handles fluid simulation for 4 compound types at a single grid square location
///   (can be repositioned as the player moves)
/// </summary>
[SceneLoadedClass("res://src/microbe_stage/CompoundCloudPlane.tscn", UsesEarlyResolve = false)]
public partial class CompoundCloudPlane : CsgMesh3D, ISaveLoadedTracked
{
    /// <summary>
    ///   The current densities of compounds. This uses custom writing so this is ignored.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Because this is such a high priority system this uses a bit more happily null suppressing than elsewhere
    ///   </para>
    /// </remarks>
    public Vector4[,] Density = null!;

    [JsonIgnore]
    public Vector4[,] OldDensity = null!;

    [JsonProperty]
    public Compound?[] Compounds = null!;

    // TODO: give each cloud (compound type) a viscosity value in the JSON file and use it instead.
    private const float VISCOSITY = 0.0525f;

    private readonly StringName brightnessParameterName = new("BrightnessMultiplier");
    private readonly StringName uvOffsetParameterName = new("UVOffset");
    private Image? image;
    private ImageTexture texture = null!;
    private FluidCurrentsSystem? fluidSystem;
    private Vector4 decayRates;

    [JsonProperty]
    private Vector2I position = new(0, 0);

    /// <summary>
    ///   To allow multithreaded operations a cached world position is needed
    /// </summary>
    [JsonProperty]
    private Vector3 cachedWorldPosition;

    [JsonProperty]
    public int Resolution { get; private set; }

    [JsonProperty]
    public int Size { get; private set; }

    public bool IsLoadedFromSave { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (!IsLoadedFromSave)
        {
            Size = Settings.Instance.CloudSimulationWidth;
            Resolution = Settings.Instance.CloudResolution;
            CreateDensityTexture();
            Density = new Vector4[Size, Size];
            OldDensity = new Vector4[Size, Size];
            ClearContents();
        }
        else
        {
            // Recreate the texture if the size changes
            // TODO: could resample the density data here to allow changing the cloud resolution or size
            // without starting a new save
            CreateDensityTexture();
            OldDensity = new Vector4[Size, Size];
            SetMaterialUVForPosition();
        }
    }

    /// <summary>
    ///   Initializes this cloud. cloud2 onwards can be null
    /// </summary>
    public void Init(FluidCurrentsSystem turbulenceSource, int renderPriority, Compound cloud1, Compound? cloud2,
        Compound? cloud3, Compound? cloud4)
    {
        fluidSystem = turbulenceSource;
        Compounds = new Compound?[Constants.CLOUDS_IN_ONE] { cloud1, cloud2, cloud3, cloud4 };

        decayRates = new Vector4(cloud1.DecayRate, cloud2?.DecayRate ?? 1.0f,
            cloud3?.DecayRate ?? 1.0f, cloud4?.DecayRate ?? 1.0f);

        var material = (ShaderMaterial)Material;

        material.SetShaderParameter("colour1", cloud1.Colour);

        var blank = new Color(0, 0, 0, 0);

        material.SetShaderParameter("colour2", cloud2?.Colour ?? blank);
        material.SetShaderParameter("colour3", cloud3?.Colour ?? blank);
        material.SetShaderParameter("colour4", cloud4?.Colour ?? blank);

        material.RenderPriority = renderPriority;
    }

    public void UpdatePosition(Vector2I newPosition)
    {
        cachedWorldPosition = Position;
        int newX = ((newPosition.X % Constants.CLOUD_SQUARES_PER_SIDE) + Constants.CLOUD_SQUARES_PER_SIDE)
            % Constants.CLOUD_SQUARES_PER_SIDE;
        int newY = ((newPosition.Y % Constants.CLOUD_SQUARES_PER_SIDE) + Constants.CLOUD_SQUARES_PER_SIDE)
            % Constants.CLOUD_SQUARES_PER_SIDE;

        if (newX == (position.X + 1) % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            PartialClearDensity(position.X * Size / Constants.CLOUD_SQUARES_PER_SIDE, 0,
                Size / Constants.CLOUD_SQUARES_PER_SIDE, Size);
        }
        else if (newX == (position.X + Constants.CLOUD_SQUARES_PER_SIDE - 1)
                 % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            PartialClearDensity(((position.X + Constants.CLOUD_SQUARES_PER_SIDE - 1)
                    % Constants.CLOUD_SQUARES_PER_SIDE) * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                0, Size / Constants.CLOUD_SQUARES_PER_SIDE, Size);
        }

        if (newY == (position.Y + 1) % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            PartialClearDensity(0, position.Y * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                Size, Size / Constants.CLOUD_SQUARES_PER_SIDE);
        }
        else if (newY == (position.Y + Constants.CLOUD_SQUARES_PER_SIDE - 1) % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            PartialClearDensity(0, ((position.Y + Constants.CLOUD_SQUARES_PER_SIDE - 1)
                    % Constants.CLOUD_SQUARES_PER_SIDE) * Size / Constants.CLOUD_SQUARES_PER_SIDE,
                Size, Size / Constants.CLOUD_SQUARES_PER_SIDE);
        }

        position = new Vector2I(newX, newY);

        // This accommodates the texture of the cloud to the new position of the plane.
        SetMaterialUVForPosition();
    }

    /// <summary>
    ///   Updates the edge concentrations of this cloud before the rest of the cloud.
    ///   This is not ran in parallel.
    /// </summary>
    public void UpdateEdgesBeforeCenter(float delta)
    {
        // The diffusion rate seems to have a bigger effect
        delta *= 100.0f;

        if (position.X != 0)
        {
            PartialDiffuseEdges(0, 0, Constants.CLOUD_EDGE_WIDTH / 2, Size, delta);
            PartialDiffuseEdges(Size - Constants.CLOUD_EDGE_WIDTH / 2, 0, Constants.CLOUD_EDGE_WIDTH / 2, Size, delta);
        }

        if (position.X != 1)
        {
            PartialDiffuseEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size, delta);
        }

        if (position.X != 2)
        {
            PartialDiffuseEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size, delta);
        }

        if (position.Y != 0)
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
            PartialDiffuseEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta);
            PartialDiffuseEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2,
                Size - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta);
        }

        if (position.Y != 1)
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

        if (position.Y != 2)
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
        delta *= 100.0f;
        var pos = new Vector2(cachedWorldPosition.X, cachedWorldPosition.Z);

        if (position.X != 0)
        {
            PartialAdvectEdges(0, 0, Constants.CLOUD_EDGE_WIDTH / 2, Size, delta, pos);
            PartialAdvectEdges(Size - Constants.CLOUD_EDGE_WIDTH / 2, 0, Constants.CLOUD_EDGE_WIDTH / 2,
                Size, delta, pos);
        }

        if (position.X != 1)
        {
            PartialAdvectEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size, delta, pos);
        }

        if (position.X != 2)
        {
            PartialAdvectEdges(2 * Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH / 2,
                0, Constants.CLOUD_EDGE_WIDTH, Size, delta, pos);
        }

        if (position.Y != 0)
        {
            PartialAdvectEdges(Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta, pos);
            PartialAdvectEdges(Constants.CLOUD_EDGE_WIDTH / 2,
                Size - Constants.CLOUD_EDGE_WIDTH / 2,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2, delta, pos);
            PartialAdvectEdges(Size / Constants.CLOUD_SQUARES_PER_SIDE + Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Size / Constants.CLOUD_SQUARES_PER_SIDE - Constants.CLOUD_EDGE_WIDTH,
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

        if (position.Y != 1)
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

        if (position.Y != 2)
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
        var pos = new Vector2(cachedWorldPosition.X, cachedWorldPosition.Z);

        for (int i = 0; i < Constants.CLOUD_SQUARES_PER_SIDE; i++)
        {
            for (int j = 0; j < Constants.CLOUD_SQUARES_PER_SIDE; j++)
            {
                var x0 = i;
                var y0 = j;

                // TODO: fix task allocations
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
        texture.Update(image);
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
    ///   Interlocked add variant that is thread safe
    /// </summary>
    public void AddCloudInterlocked(Compound compound, int x, int y, float density)
    {
        var compoundIndex = GetCompoundIndex(compound);

        float seenCurrentAmount;
        float newValue;

        switch (compoundIndex)
        {
            case 0:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].X;
                    newValue = seenCurrentAmount + density;
                }
                while (System.Threading.Interlocked.CompareExchange(ref Density[x, y].X,
                    newValue, seenCurrentAmount) != seenCurrentAmount);

                break;
            }

            case 1:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].Y;
                    newValue = seenCurrentAmount + density;
                }
                while (System.Threading.Interlocked.CompareExchange(ref Density[x, y].Y,
                    newValue, seenCurrentAmount) != seenCurrentAmount);

                break;
            }

            case 2:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].Z;
                    newValue = seenCurrentAmount + density;
                }
                while (System.Threading.Interlocked.CompareExchange(ref Density[x, y].Z,
                 newValue, seenCurrentAmount) != seenCurrentAmount);

                break;
            }

            case 3:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].W;
                    newValue = seenCurrentAmount + density;
                }
                while (System.Threading.Interlocked.CompareExchange(ref Density[x, y].W,
                    newValue, seenCurrentAmount) != seenCurrentAmount);

                break;
            }

            default:
                throw new ArgumentException("This cloud doesn't handle the given compound type");
        }
    }

    /// <summary>
    ///   Add cloud variant that ignores unhandled compound types
    /// </summary>
    /// <returns>True if added, false if this didn't handle the given type</returns>
    public bool AddCloudInterlockedIfHandlesType(Compound compound, int x, int y, float density)
    {
        var compoundIndex = GetCompoundIndex(compound);

        float seenCurrentAmount;
        float newValue;

        // Exact comparisons used to know when the atomic operation really succeeded
        // ReSharper disable CompareOfFloatsByEqualityOperator
        switch (compoundIndex)
        {
            case 0:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].X;
                    newValue = seenCurrentAmount + density;
                }
                while (System.Threading.Interlocked.CompareExchange(ref Density[x, y].X,
                    newValue, seenCurrentAmount) != seenCurrentAmount);

                return true;
            }

            case 1:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].Y;
                    newValue = seenCurrentAmount + density;
                }
                while (System.Threading.Interlocked.CompareExchange(ref Density[x, y].Y,
                    newValue, seenCurrentAmount) != seenCurrentAmount);

                return true;
            }

            case 2:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].Z;
                    newValue = seenCurrentAmount + density;
                }
                while (System.Threading.Interlocked.CompareExchange(ref Density[x, y].Z,
                    newValue, seenCurrentAmount) != seenCurrentAmount);

                return true;
            }

            case 3:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].W;
                    newValue = seenCurrentAmount + density;
                }
                while (System.Threading.Interlocked.CompareExchange(ref Density[x, y].W,
                    newValue, seenCurrentAmount) != seenCurrentAmount);

                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>
    ///   Takes some amount of compound, in cloud local coordinates.
    /// </summary>
    /// <returns>The amount of compound taken</returns>
    public float TakeCompound(Compound compound, int x, int y, float fraction = 1.0f)
    {
        float amountInCloud = HackyAddress(ref Density[x, y], GetCompoundIndex(compound));
        var amountToGive = amountInCloud * fraction;

        if (amountInCloud - amountToGive < 0.1f)
        {
            // Taking basically everything in the cloud
            Density[x, y] += CalculateCloudToAdd(compound, -amountInCloud);
        }
        else
        {
            Density[x, y] += CalculateCloudToAdd(compound, -amountToGive);
        }

        return amountToGive;
    }

    /// <summary>
    ///   Multithreading safe TakeCompound variant
    /// </summary>
    /// <returns>
    ///   True if the interlocked exchange succeeded, false if the <see cref="seenCurrentAmount"/> needs to be re-read
    ///   and this re-attempted
    /// </returns>
    public bool TakeCompoundInterlocked(int compoundIndex, int x, int y, float fraction, float seenCurrentAmount,
        out float taken)
    {
        taken = seenCurrentAmount * fraction;
        float newValue;

        if (seenCurrentAmount - taken < 0.1f)
        {
            newValue = 0;
        }
        else
        {
            newValue = seenCurrentAmount - taken;
        }

        switch (compoundIndex)
        {
            case 0:
                return System.Threading.Interlocked.CompareExchange(ref Density[x, y].X, newValue, seenCurrentAmount) ==
                    seenCurrentAmount;
            case 1:
                return System.Threading.Interlocked.CompareExchange(ref Density[x, y].Y, newValue, seenCurrentAmount) ==
                    seenCurrentAmount;
            case 2:
                return System.Threading.Interlocked.CompareExchange(ref Density[x, y].Z, newValue, seenCurrentAmount) ==
                    seenCurrentAmount;
            case 3:
                return System.Threading.Interlocked.CompareExchange(ref Density[x, y].W, newValue, seenCurrentAmount) ==
                    seenCurrentAmount;
            default:
                throw new ArgumentException("Compound index out of range");
        }
    }

    /// <summary>
    ///   Calculates how much TakeCompound would take without actually taking the amount
    /// </summary>
    /// <returns>The amount available for taking</returns>
    public float AmountAvailable(Compound compound, int x, int y, float fraction = 1.0f)
    {
        float amountInCloud = HackyAddress(ref Density[x, y], GetCompoundIndex(compound));
        float amountToGive = amountInCloud * fraction;
        return amountToGive;
    }

    /// <summary>
    ///   Returns all the compounds that are available at point
    /// </summary>
    public void GetCompoundsAt(int x, int y, Dictionary<Compound, float> result, bool onlyAbsorbable)
    {
        for (int i = 0; i < Constants.CLOUDS_IN_ONE; i++)
        {
            var compound = Compounds[i];
            if (compound == null)
                break;

            if (!compound.IsAbsorbable && onlyAbsorbable)
                continue;

            float amount = HackyAddress(ref Density[x, y], i);
            if (amount > 0)
                result[compound] = amount;
        }
    }

    /// <summary>
    ///   Returns true if position with radius around it contains any
    ///   points that are within this cloud.
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
        if (worldPosition.X + radius < cachedWorldPosition.X - Constants.CLOUD_WIDTH ||
            worldPosition.X - radius >= cachedWorldPosition.X + Constants.CLOUD_WIDTH ||
            worldPosition.Z + radius < cachedWorldPosition.Z - Constants.CLOUD_HEIGHT ||
            worldPosition.Z - radius >= cachedWorldPosition.Z + Constants.CLOUD_HEIGHT)
            return false;

        return true;
    }

    /// <summary>
    ///   Converts world coordinate to cloud relative (top left) coordinates
    /// </summary>
    public void ConvertToCloudLocal(Vector3 worldPosition, out int x, out int y)
    {
        var topLeftRelative = worldPosition - cachedWorldPosition;

        x = ((int)Math.Floor((topLeftRelative.X + Constants.CLOUD_WIDTH) / Resolution)
            + position.X * Size / Constants.CLOUD_SQUARES_PER_SIDE) % Size;
        y = ((int)Math.Floor((topLeftRelative.Z + Constants.CLOUD_HEIGHT) / Resolution)
            + position.Y * Size / Constants.CLOUD_SQUARES_PER_SIDE) % Size;
    }

    [SuppressMessage("ReSharper", "PossibleLossOfFraction",
        Justification = "Adding floats casts in the function might do more harm then good")]
    public Vector3 ConvertToWorld(int cloudX, int cloudY)
    {
        return new Vector3(cloudX * Resolution +
            ((4 - position.X) % 3 - 1) * Resolution * Size / Constants.CLOUD_SQUARES_PER_SIDE -
            Constants.CLOUD_WIDTH,
            0,
            cloudY * Resolution + ((4 - position.Y) % 3 - 1) * Resolution * Size / Constants.CLOUD_SQUARES_PER_SIDE -
            Constants.CLOUD_HEIGHT) + cachedWorldPosition;
    }

    /// <summary>
    ///   Absorbs compounds from this cloud. Doesn't require locking thanks to using atomic updates.
    /// </summary>
    public void AbsorbCompounds(int localX, int localY, CompoundBag storage,
        Dictionary<Compound, float>? totals, float delta, float rate)
    {
        if (rate < 0)
            throw new ArgumentException("Rate can't be negative");

        var fractionToTake = 1.0f - (float)Math.Pow(0.5f, delta / Constants.CLOUD_ABSORPTION_HALF_LIFE);
        float radius = 1.0f;
        float radiusSquared = radius * radius;

        for (int i = 0; i < Constants.CLOUDS_IN_ONE; i++)
        {
            var compound = Compounds[i];
            if (compound == null)
                break;

            if (!compound.IsAbsorbable || !storage.IsUseful(compound))
                continue;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx * dx + dy * dy > radiusSquared)
                        continue;

                    int x = localX + dx;
                    int y = localY + dy;

                    if (x < 0 || y < 0 || x >= Size || y >= Size)
                        continue;

                    while (true)
                    {
                        float cloudAmount = HackyAddress(ref Density[x, y], i);
                        float generousAmount = cloudAmount * Constants.SKIP_TRYING_TO_ABSORB_RATIO;

                        if (generousAmount < MathUtils.EPSILON)
                            break;

                        float freeSpace = storage.GetFreeSpaceForCompound(compound);
                        float multiplier = 1.0f * rate;

                        if (freeSpace < generousAmount)
                        {
                            if (freeSpace < 0.0f)
                                throw new InvalidOperationException("Free space for compounds is negative");

                            multiplier = freeSpace / generousAmount;

                        }

                        if (!TakeCompoundInterlocked(i, x, y, fractionToTake * multiplier, cloudAmount,
                                out float taken))
                        {
                            continue;
                        }

                        taken *= Constants.ABSORPTION_RATIO;
                        storage.AddCompound(compound, taken);

                        if (totals != null)
                        {
                            totals.TryGetValue(compound, out var existingValue);
                            totals[compound] = existingValue + taken;
                        }

                        break;
                    }
                }
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

    public void SetBrightness(float brightness)
    {
        var material = (ShaderMaterial)Material;
        material.SetShaderParameter(brightnessParameterName, brightness);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (image != null)
            {
                brightnessParameterName.Dispose();
                uvOffsetParameterName.Dispose();
                image.Dispose();
                texture.Dispose();
            }
        }

        base.Dispose(disposing);
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

    private Vector4 CalculateCloudToAdd(Compound compound, float density)
    {
        return new Vector4(Compounds[0] == compound ? density : 0.0f,
            Compounds[1] == compound ? density : 0.0f,
            Compounds[2] == compound ? density : 0.0f,
            Compounds[3] == compound ? density : 0.0f);
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
                    var velocity = fluidSystem!.VelocityAt(pos + new Vector2(x, y) * Resolution) * VISCOSITY;

                    // This is run in parallel, this may not touch the other compound clouds
                    float dx = x + (delta * velocity.X);
                    float dy = y + (delta * velocity.Y);

                    // So this is clamped to not go to the other clouds
                    dx = dx.Clamp(x0 - 0.5f, x0 + width + 0.5f);
                    dy = dy.Clamp(y0 - 0.5f, y0 + height + 0.5f);

                    CalculateMovementFactors(dx, dy, out var q0, out var q1, out var r0, out var r1,
                        out var s1, out var s0, out var t1, out var t0);

                    // NOTE: we add modulo to avoid overflow due to large time steps
                    // This makes this function a duplicate of PartialAdvectEdges
                    // TODO: check for refactorization (and in general of the whole file) --Maxonovien
                    q0 = q0.PositiveModulo(Size);
                    q1 = q1.PositiveModulo(Size);
                    r0 = r0.PositiveModulo(Size);
                    r1 = r1.PositiveModulo(Size);

                    Density[q0, r0] += OldDensity[x, y] * decayRates * s0 * t0;
                    Density[q0, r1] += OldDensity[x, y] * decayRates * s0 * t1;
                    Density[q1, r0] += OldDensity[x, y] * decayRates * s1 * t0;
                    Density[q1, r1] += OldDensity[x, y] * decayRates * s1 * t1;
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
                    var velocity = fluidSystem!.VelocityAt(pos + new Vector2(x, y) * Resolution) * VISCOSITY;

                    // This is run in parallel, this may not touch the other compound clouds
                    float dx = x + (delta * velocity.X);
                    float dy = y + (delta * velocity.Y);

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
                image!.SetPixel(x, y, new Color(pixel.X, pixel.Y, pixel.Z, pixel.W));
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

    private float HackyAddress(ref Vector4 vector, int index)
    {
        switch (index)
        {
            case 0:
                return vector.X;
            case 1:
                return vector.Y;
            case 2:
                return vector.Z;
            case 3:
                return vector.W;
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
        image = Image.Create(Size, Size, false, Image.Format.Rgba8);
        texture = ImageTexture.CreateFromImage(image);

        var material = (ShaderMaterial)Material;
        material.SetShaderParameter("densities", texture);
    }

    private void SetMaterialUVForPosition()
    {
        var material = (ShaderMaterial)Material;
        material.SetShaderParameter(uvOffsetParameterName, new Vector2(
            position.X / (float)Constants.CLOUD_SQUARES_PER_SIDE,
            position.Y / (float)Constants.CLOUD_SQUARES_PER_SIDE));
    }
}
