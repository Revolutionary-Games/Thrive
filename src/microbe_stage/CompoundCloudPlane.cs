using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using SharedBase.Archive;
using Systems;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;
using Vector4 = System.Numerics.Vector4;

/// <summary>
///   A single compound cloud plane that handles fluid simulation for 4 compound types at a single grid square location
///   (can be repositioned as the player moves)
/// </summary>
public partial class CompoundCloudPlane : MeshInstance3D, ISaveLoadedTracked, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   The current densities of compounds. This uses custom writing, so this is ignored.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Because this is such a high-priority system, this uses a bit more happily null suppressing than elsewhere
    ///   </para>
    /// </remarks>
    public Vector4[,] Density = null!;

    public Vector4[,] OldDensity = null!;

    public Compound[] Compounds = null!;

    // TODO: give each cloud (compound type) a viscosity value in the JSON file and use it instead.
    private const float VISCOSITY = 0.0525f;

    private readonly StringName brightnessParameterName = new("BrightnessMultiplier");
    private readonly StringName uvOffsetParameterName = new("UVOffset");

    private CompoundDefinition?[] compoundDefinitions = null!;

    private Image? image;
    private ImageTexture texture = null!;
    private FluidCurrentsSystem? fluidSystem;

    private Vector4 decayRates;

    /// <summary>
    ///   Which square plane player is in
    /// </summary>
    private Vector2I playersPosition = new(0, 0);

    /// <summary>
    ///   To allow multithreaded operations, a cached world position is needed
    /// </summary>
    private Vector3 cachedWorldPosition;

    public int CloudResolution { get; private set; }

    public int PlaneSize { get; private set; }

    public bool IsLoadedFromSave { get; set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPlane;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPlane)
            throw new NotSupportedException();

        writer.WriteObject((CompoundCloudPlane)obj);
    }

    public static CompoundCloudPlane ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var scene = GD.Load<PackedScene>("res://src/microbe_stage/CompoundCloudPlane.tscn");

        var instance = scene.Instantiate<CompoundCloudPlane>();

        instance.Compounds = reader.ReadObject<Compound[]>();
        instance.playersPosition = reader.ReadVector2I();
        instance.cachedWorldPosition = reader.ReadVector3();
        instance.CloudResolution = reader.ReadInt32();
        instance.PlaneSize = reader.ReadInt32();
        instance.Position = reader.ReadVector3();

        // Then the density data
        var buffer = new byte[instance.PlaneSize * 4 * 4];

        int dimensions = instance.PlaneSize;

        var target = new Vector4[dimensions, dimensions];

        for (int x = 0; x < dimensions; ++x)
        {
            // Read this line's buffer
            reader.ReadBytes(buffer.AsSpan());
            int bufferReadOffset = 0;

            for (int y = 0; y < dimensions; ++y)
            {
                // Then reconstruct each data item
                var vector4 = default(Vector4);

                var data = buffer[bufferReadOffset++] | (uint)buffer[bufferReadOffset++] << 8 |
                    (uint)buffer[bufferReadOffset++] << 16 | (uint)buffer[bufferReadOffset++] << 24;
                vector4.X = BitConverter.UInt32BitsToSingle(data);

                data = buffer[bufferReadOffset++] | (uint)buffer[bufferReadOffset++] << 8 |
                    (uint)buffer[bufferReadOffset++] << 16 | (uint)buffer[bufferReadOffset++] << 24;
                vector4.Y = BitConverter.UInt32BitsToSingle(data);

                data = buffer[bufferReadOffset++] | (uint)buffer[bufferReadOffset++] << 8 |
                    (uint)buffer[bufferReadOffset++] << 16 | (uint)buffer[bufferReadOffset++] << 24;
                vector4.Z = BitConverter.UInt32BitsToSingle(data);

                data = buffer[bufferReadOffset++] | (uint)buffer[bufferReadOffset++] << 8 |
                    (uint)buffer[bufferReadOffset++] << 16 | (uint)buffer[bufferReadOffset++] << 24;
                vector4.W = BitConverter.UInt32BitsToSingle(data);

                target[x, y] = vector4;
            }

            if (bufferReadOffset != buffer.Length)
                throw new Exception("Buffer read offset did not reach the end of the buffer");
        }

        instance.Density = target;

        instance.IsLoadedFromSave = true;

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Compounds);
        writer.Write(playersPosition);
        writer.Write(cachedWorldPosition);
        writer.Write(CloudResolution);
        writer.Write(PlaneSize);
        writer.Write(Position);

        var localDensity = Density;

        // If rank changes square root is not suitable
        if (localDensity.Rank != 2)
            throw new Exception("Cloud plane densities array rank is not 2");

        int dimensions = (int)Math.Sqrt(localDensity.Length);

        if (dimensions != PlaneSize)
            throw new Exception("Cloud plane size invariants have changed");

        // To avoid blowing the stack, we need to allocate temporary buffer memory here so that we can write all the
        // density data in large chunks. We could maybe precompress the data here, but saves are stored in a compressed
        // container anyway, so for now that is skipped.

        // Each Vector4 is 4 floats, so 4 x 4 bytes
        var buffer = new byte[dimensions * 4 * 4];

        // TODO: it might be interesting to compare the performance if each individual float was written separately to
        // the archive whether that is faster than bulk preparing everything.
        for (int x = 0; x < dimensions; ++x)
        {
            int bufferOffset = 0;

            // Convert data into the buffer
            for (int y = 0; y < dimensions; ++y)
            {
                var vector4 = localDensity[x, y];

                var data = BitConverter.SingleToUInt32Bits(vector4.X);

                buffer[bufferOffset++] = (byte)data;
                buffer[bufferOffset++] = (byte)(data >> 8);
                buffer[bufferOffset++] = (byte)(data >> 16);
                buffer[bufferOffset++] = (byte)(data >> 24);

                data = BitConverter.SingleToUInt32Bits(vector4.Y);

                buffer[bufferOffset++] = (byte)data;
                buffer[bufferOffset++] = (byte)(data >> 8);
                buffer[bufferOffset++] = (byte)(data >> 16);
                buffer[bufferOffset++] = (byte)(data >> 24);

                data = BitConverter.SingleToUInt32Bits(vector4.Z);

                buffer[bufferOffset++] = (byte)data;
                buffer[bufferOffset++] = (byte)(data >> 8);
                buffer[bufferOffset++] = (byte)(data >> 16);
                buffer[bufferOffset++] = (byte)(data >> 24);

                data = BitConverter.SingleToUInt32Bits(vector4.W);

                buffer[bufferOffset++] = (byte)data;
                buffer[bufferOffset++] = (byte)(data >> 8);
                buffer[bufferOffset++] = (byte)(data >> 16);
                buffer[bufferOffset++] = (byte)(data >> 24);
            }

            if (bufferOffset != buffer.Length)
                throw new Exception("Buffer offset did not reach the end of the buffer");

            // Then write the entire buffer
            writer.Write(buffer);
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (!IsLoadedFromSave)
        {
            PlaneSize = Settings.Instance.CloudSimulationWidth;
            CloudResolution = Settings.Instance.CloudResolution;
            CreateDensityTexture();

            Density = new Vector4[PlaneSize, PlaneSize];
            OldDensity = new Vector4[PlaneSize, PlaneSize];
            ClearContents();
        }
        else
        {
            // Recreate the texture if the size changes
            // TODO: could resample the density data here to allow changing the cloud resolution or size
            // without starting a new save
            CreateDensityTexture();

            OldDensity = new Vector4[PlaneSize, PlaneSize];
            SetMaterialUVForPosition();
        }
    }

    /// <summary>
    ///   Initializes this cloud. cloud2 onwards can be <see cref="Compound.Invalid"/>
    /// </summary>
    public void Init(FluidCurrentsSystem turbulenceSource, int renderPriority, Compound cloud1, Compound cloud2,
        Compound cloud3, Compound cloud4)
    {
        if (cloud1 == Compound.Invalid)
            throw new ArgumentException("First cloud type must be a valid compound");

        fluidSystem = turbulenceSource;

        // This is defined with the full syntax to ensure this size ends up always at 4
        Compounds = new Compound[Constants.CLOUDS_IN_ONE] { cloud1, cloud2, cloud3, cloud4 };

        var simulationParameters = SimulationParameters.Instance;

        CompoundDefinition cloud1Definition = simulationParameters.GetCompoundDefinition(cloud1);
        CompoundDefinition? cloud2Definition = null;
        CompoundDefinition? cloud3Definition = null;
        CompoundDefinition? cloud4Definition = null;

        if (cloud2 != Compound.Invalid)
            cloud2Definition = simulationParameters.GetCompoundDefinition(cloud2);

        if (cloud3 != Compound.Invalid)
            cloud3Definition = simulationParameters.GetCompoundDefinition(cloud3);

        if (cloud4 != Compound.Invalid)
            cloud4Definition = simulationParameters.GetCompoundDefinition(cloud4);

        compoundDefinitions = [cloud1Definition, cloud2Definition, cloud3Definition, cloud4Definition];

        decayRates = new Vector4(cloud1Definition.DecayRate, cloud2Definition?.DecayRate ?? 1.0f,
            cloud3Definition?.DecayRate ?? 1.0f, cloud4Definition?.DecayRate ?? 1.0f);

        // Setup colours
        var material = (ShaderMaterial)MaterialOverride;

        material.SetShaderParameter("colour1", cloud1Definition.Colour);

        var blank = new Color(0, 0, 0, 0);

        material.SetShaderParameter("colour2", cloud2Definition?.Colour ?? blank);
        material.SetShaderParameter("colour3", cloud3Definition?.Colour ?? blank);
        material.SetShaderParameter("colour4", cloud4Definition?.Colour ?? blank);

        // CompoundCloudPlanes need different render priorities to avoid the flickering effect
        // Which result from intersecting meshes.
        material.RenderPriority = renderPriority;
    }

    public void UpdatePosition(Vector2I newPosition)
    {
        cachedWorldPosition = Position;

        int newX = newPosition.X.PositiveModulo(Constants.CLOUD_PLANE_SQUARES_PER_SIDE);
        int newY = newPosition.Y.PositiveModulo(Constants.CLOUD_PLANE_SQUARES_PER_SIDE);

        if (newX == (playersPosition.X + 1) % Constants.CLOUD_PLANE_SQUARES_PER_SIDE)
        {
            PartialClearDensity(playersPosition.X * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE, 0,
                PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE, PlaneSize);
        }
        else if (newX == (playersPosition.X + Constants.CLOUD_PLANE_SQUARES_PER_SIDE - 1)
                 % Constants.CLOUD_PLANE_SQUARES_PER_SIDE)
        {
            PartialClearDensity(((playersPosition.X + Constants.CLOUD_PLANE_SQUARES_PER_SIDE - 1)
                    % Constants.CLOUD_PLANE_SQUARES_PER_SIDE) * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                0, PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE, PlaneSize);
        }

        if (newY == (playersPosition.Y + 1) % Constants.CLOUD_PLANE_SQUARES_PER_SIDE)
        {
            PartialClearDensity(0, playersPosition.Y * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                PlaneSize, PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE);
        }
        else if (newY == (playersPosition.Y + Constants.CLOUD_PLANE_SQUARES_PER_SIDE - 1) %
                 Constants.CLOUD_PLANE_SQUARES_PER_SIDE)
        {
            PartialClearDensity(0, ((playersPosition.Y + Constants.CLOUD_PLANE_SQUARES_PER_SIDE - 1)
                    % Constants.CLOUD_PLANE_SQUARES_PER_SIDE) * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                PlaneSize, PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE);
        }

        playersPosition = new Vector2I(newX, newY);

        // This accommodates the texture of the cloud to the new position of the plane.
        SetMaterialUVForPosition();
    }

    /// <summary>
    ///   Updates the edge concentrations of this cloud before the rest of the cloud.
    ///   This is not ran in parallel.
    /// </summary>
    public void DiffuseEdges(float delta)
    {
        // Increase diffusion effect
        delta *= 100.0f;

        int edgeWidth = Constants.CLOUD_PLANE_EDGE_WIDTH;
        int halfEdgeWidth = edgeWidth / 2;
        int cellSize = PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE;

        // Vertical edge columns
        PartialDiffuse(0, 0, halfEdgeWidth, PlaneSize, delta);
        PartialDiffuse(1 * cellSize - halfEdgeWidth, 0, edgeWidth, PlaneSize, delta);
        PartialDiffuse(2 * cellSize - halfEdgeWidth, 0, edgeWidth, PlaneSize, delta);
        PartialDiffuse(3 * cellSize - halfEdgeWidth, 0, halfEdgeWidth, PlaneSize, delta);

        // Horizontal edge rows
        for (int square = 0; square < Constants.CLOUD_PLANE_SQUARES_PER_SIDE; ++square)
        {
            int x = square * cellSize + halfEdgeWidth;
            int width = cellSize - edgeWidth;

            PartialDiffuse(x, 3 * cellSize - halfEdgeWidth, width, halfEdgeWidth, delta);
            PartialDiffuse(x, 2 * cellSize - halfEdgeWidth, width, edgeWidth, delta);
            PartialDiffuse(x, 1 * cellSize - halfEdgeWidth, width, edgeWidth, delta);
            PartialDiffuse(x, 0, width, halfEdgeWidth, delta);
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueDiffuseCloud(float delta, List<Task> queue)
    {
        // The diffusion rate seems to have a bigger effect
        delta *= 100.0f;

        for (int i = 0; i < Constants.CLOUD_PLANE_SQUARES_PER_SIDE; ++i)
        {
            for (int j = 0; j < Constants.CLOUD_PLANE_SQUARES_PER_SIDE; ++j)
            {
                var x0 = i;
                var y0 = j;

                // TODO: fix task allocations
                var task = new Task(() => PartialDiffuseCenter(x0 * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    y0 * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    delta));
                queue.Add(task);
            }
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueAdvectCloud(float delta, List<Task> queue)
    {
        // The diffusion rate seems to have a bigger effect
        delta *= 100.0f;

        for (int i = 0; i < Constants.CLOUD_PLANE_SQUARES_PER_SIDE; ++i)
        {
            for (int j = 0; j < Constants.CLOUD_PLANE_SQUARES_PER_SIDE; ++j)
            {
                var x0 = i;
                var y0 = j;

                // TODO: fix task allocations
                var task = new Task(() => PartialAdvect(x0 * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    y0 * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    delta));
                queue.Add(task);
            }
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueUpdateTextureImage(List<Task> queue)
    {
        for (int i = 0; i < Constants.CLOUD_PLANE_SQUARES_PER_SIDE; ++i)
        {
            for (int j = 0; j < Constants.CLOUD_PLANE_SQUARES_PER_SIDE; ++j)
            {
                var x0 = i;
                var y0 = j;

                // TODO: fix task allocations
                var task = new Task(() => PartialUpdateTextureImage(
                    x0 * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    y0 * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
                    PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE));
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
                while (Interlocked.CompareExchange(ref Density[x, y].X, newValue, seenCurrentAmount) !=
                       seenCurrentAmount);

                break;
            }

            case 1:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].Y;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x, y].Y, newValue, seenCurrentAmount) !=
                       seenCurrentAmount);

                break;
            }

            case 2:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].Z;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x, y].Z, newValue, seenCurrentAmount) !=
                       seenCurrentAmount);

                break;
            }

            case 3:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].W;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x, y].W, newValue, seenCurrentAmount) !=
                       seenCurrentAmount);

                break;
            }

            default:
                throw new ArgumentException("This cloud doesn't handle the given compound type");
        }

        // ReSharper restore CompareOfFloatsByEqualityOperator
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
                while (Interlocked.CompareExchange(ref Density[x, y].X, newValue, seenCurrentAmount) !=
                       seenCurrentAmount);

                return true;
            }

            case 1:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].Y;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x, y].Y, newValue, seenCurrentAmount) !=
                       seenCurrentAmount);

                return true;
            }

            case 2:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].Z;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x, y].Z, newValue, seenCurrentAmount) !=
                       seenCurrentAmount);

                return true;
            }

            case 3:
            {
                do
                {
                    seenCurrentAmount = Density[x, y].W;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x, y].W, newValue, seenCurrentAmount) !=
                       seenCurrentAmount);

                return true;
            }

            default:
                return false;
        }

        // ReSharper restore CompareOfFloatsByEqualityOperator
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
            // Taking basically everything in the cloud
            newValue = 0;
        }
        else
        {
            newValue = seenCurrentAmount - taken;
        }

        // Exact comparisons used to know when the atomic operation really succeeded
        // ReSharper disable CompareOfFloatsByEqualityOperator
        switch (compoundIndex)
        {
            case 0:
                return Interlocked.CompareExchange(ref Density[x, y].X, newValue, seenCurrentAmount) ==
                    seenCurrentAmount;
            case 1:
                return Interlocked.CompareExchange(ref Density[x, y].Y, newValue, seenCurrentAmount) ==
                    seenCurrentAmount;
            case 2:
                return Interlocked.CompareExchange(ref Density[x, y].Z, newValue, seenCurrentAmount) ==
                    seenCurrentAmount;
            case 3:
                return Interlocked.CompareExchange(ref Density[x, y].W, newValue, seenCurrentAmount) ==
                    seenCurrentAmount;
            default:
                throw new ArgumentException("Compound index out of range");
        }

        // ReSharper restore CompareOfFloatsByEqualityOperator
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
        for (int i = 0; i < Constants.CLOUDS_IN_ONE; ++i)
        {
            var compound = Compounds[i];
            if (compound == Compound.Invalid)
                break;

            if (onlyAbsorbable && !compoundDefinitions[i]!.IsAbsorbable)
                continue;

            float amount = HackyAddress(ref Density[x, y], i);
            if (amount > 0)
                result[compound] = amount;
        }
    }

    /// <summary>
    ///   Checks if position is in this cloud, also returns relative coordinates
    /// </summary>
    public bool ContainsPosition(Vector3 worldPosition, out int x, out int y)
    {
        ConvertToCloudLocal(worldPosition, out x, out y);
        return x >= 0 && y >= 0 && x < Constants.CLOUD_SIZE && y < Constants.CLOUD_SIZE;
    }

    /// <summary>
    ///   Returns true if position with radius around it contains any
    ///   points that are within this cloud.
    /// </summary>
    public bool ContainsPositionWithRadius(Vector3 worldPosition,
        float radius)
    {
        if (worldPosition.X + radius < cachedWorldPosition.X - Constants.CLOUD_SIZE ||
            worldPosition.X - radius >= cachedWorldPosition.X + Constants.CLOUD_SIZE ||
            worldPosition.Z + radius < cachedWorldPosition.Z - Constants.CLOUD_SIZE ||
            worldPosition.Z - radius >= cachedWorldPosition.Z + Constants.CLOUD_SIZE)
            return false;

        return true;
    }

    /// <summary>
    ///   Converts world coordinate to cloud relative (top left) coordinates
    /// </summary>
    public void ConvertToCloudLocal(Vector3 worldPosition, out int x, out int y)
    {
        var topLeftRelative = worldPosition - cachedWorldPosition;

        // Floor is used here because otherwise the last coordinate is wrong
        x = ((int)Math.Floor((topLeftRelative.X + Constants.CLOUD_SIZE) / CloudResolution)
            + playersPosition.X * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE) % PlaneSize;
        y = ((int)Math.Floor((topLeftRelative.Z + Constants.CLOUD_SIZE) / CloudResolution)
            + playersPosition.Y * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE) % PlaneSize;
    }

    /// <summary>
    ///   Converts cloud local coordinates to world coordinates
    /// </summary>
    public Vector3 ConvertToWorld(int cloudX, int cloudY)
    {
        // Integer calculations are intentional here
        // ReSharper disable PossibleLossOfFraction
        return new Vector3(cloudX * CloudResolution +
            ((4 - playersPosition.X) % 3 - 1) * CloudResolution * PlaneSize /
            Constants.CLOUD_PLANE_SQUARES_PER_SIDE -
            Constants.CLOUD_SIZE,
            0,
            cloudY * CloudResolution + ((4 - playersPosition.Y) % 3 - 1) *
            CloudResolution * PlaneSize /
            Constants.CLOUD_PLANE_SQUARES_PER_SIDE -
            Constants.CLOUD_SIZE) + cachedWorldPosition;

        // ReSharper restore PossibleLossOfFraction
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

        for (int i = 0; i < Constants.CLOUDS_IN_ONE; ++i)
        {
            var compound = Compounds[i];
            if (compound == Compound.Invalid)
                break;

            // Skip if compound is non-useful or disallowed to be absorbed
            if (!compoundDefinitions[i]!.IsAbsorbable
                || (!storage.IsUseful(compound) && !compoundDefinitions[i]!.AlwaysAbsorbable))
            {
                continue;
            }

            // Loop here to retry in case we read stale data
            while (true)
            {
                // Overestimate of how much compounds we get
                float cloudAmount = HackyAddress(ref Density[localX, localY], i);
                float generousAmount = cloudAmount * Constants.SKIP_TRYING_TO_ABSORB_RATIO;

                // Skip if there isn't enough to absorb
                if (generousAmount < MathUtils.EPSILON)
                    break;

                float freeSpace = storage.GetFreeSpaceForCompound(compound);

                float multiplier = 1.0f * rate;

                if (freeSpace < generousAmount)
                {
                    if (freeSpace < 0.0f)
                        throw new InvalidOperationException("Free space for compounds is negative");

                    // Allow partial absorption to allow cells to take from high density clouds
                    multiplier = freeSpace / generousAmount;
                }

                if (!TakeCompoundInterlocked(i, localX, localY, fractionToTake * multiplier, cloudAmount,
                        out float taken))
                {
                    // Value was updated since we read it, we need to retry
                    continue;
                }

                taken *= Constants.ABSORPTION_RATIO;

                // This should never fail to add the full amount of compounds as we checked the free space above and
                // scaled the take amount accordingly
                storage.AddCompound(compound, taken);

                if (totals != null)
                {
                    // Keep track of total compounds absorbed for the cell
                    totals.TryGetValue(compound, out var existingValue);
                    totals[compound] = existingValue + taken;
                }

                break;
            }
        }
    }

    public void ClearContents()
    {
        Array.Clear(Density, 0, Density.Length);
        Array.Clear(OldDensity, 0, OldDensity.Length);
    }

    public void SetBrightness(float brightness)
    {
        var material = (ShaderMaterial)MaterialOverride;
        material.SetShaderParameter(brightnessParameterName, brightness);
    }

    public void ClearDensity()
    {
        Array.Clear(Density, 0, Density.Length);
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
    private static void CalculateMovementFactors(float dx, float dy, out int floorX, out int ceilX, out int floorY,
        out int ceilY, out float weightRight, out float weightLeft, out float weightBottom, out float weightTop)
    {
        floorX = (int)Math.Floor(dx);
        ceilX = floorX + 1;
        floorY = (int)Math.Floor(dy);
        ceilY = floorY + 1;

        weightRight = Math.Abs(dx - floorX);
        weightLeft = 1.0f - weightRight;
        weightBottom = Math.Abs(dy - floorY);
        weightTop = 1.0f - weightBottom;
    }

    private Vector4 CalculateCloudToAdd(Compound compound, float density)
    {
        return new Vector4(Compounds[0] == compound ? density : 0.0f,
            Compounds[1] == compound ? density : 0.0f,
            Compounds[2] == compound ? density : 0.0f,
            Compounds[3] == compound ? density : 0.0f);
    }

    private void PartialDiffuse(int x0, int y0, int width, int height, float delta)
    {
        float a = delta * Constants.CLOUD_DIFFUSION_RATE;

        for (int x = x0; x < x0 + width; ++x)
        {
            for (int y = y0; y < y0 + height; ++y)
            {
                OldDensity[x, y] =
                    Density[x, y] * (1 - a) +
                    (
                        Density[x, (y - 1 + PlaneSize) % PlaneSize] +
                        Density[x, (y + 1) % PlaneSize] +
                        Density[(x - 1 + PlaneSize) % PlaneSize, y] +
                        Density[(x + 1) % PlaneSize, y]) * (a * 0.25f);
            }
        }
    }

    private Vector3 GetWorldPositionForAdvection(int x0, int y0)
    {
        int worldShift = Constants.CLOUD_SIZE / Constants.CLOUD_PLANE_SQUARES_PER_SIDE * CloudResolution;
        int xShift = GetEdgeShift(x0, playersPosition.X);
        int yShift = GetEdgeShift(y0, playersPosition.Y);

        var wholePlaneShift = new Vector3(worldShift * ((4 - playersPosition.X) % 3 - 1) - Constants.CLOUD_SIZE, 0,
            worldShift * ((4 - playersPosition.Y) % 3 - 1) - Constants.CLOUD_SIZE);
        var edgePlanesShift = new Vector3(xShift * worldShift, 0, yShift * worldShift);

        return cachedWorldPosition + wholePlaneShift + edgePlanesShift;
    }

    private int GetEdgeShift(int coord, int playerPos)
    {
        if (coord == 0 && playerPos == 1)
            return 3;
        if (coord == 2 * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE && playerPos == 2)
            return -3;

        return 0;
    }

    private void PartialAdvect(int x0, int y0, int width, int height, float delta)
    {
        var resolution = CloudResolution;
        var worldPos = GetWorldPositionForAdvection(x0, y0);

        for (int x = x0; x < x0 + width; ++x)
        {
            for (int y = y0; y < y0 + height; ++y)
            {
                var oldDensity = OldDensity[x, y];
                if (oldDensity.LengthSquared() <= 1)
                    continue;

                var velocity =
                    fluidSystem!.VelocityAt(new Vector2(worldPos.X + x * resolution, worldPos.Z + y * resolution));

                if (MathF.Abs(velocity.X) + MathF.Abs(velocity.Y) <
                    Constants.CURRENT_COMPOUND_CLOUD_ADVECT_THRESHOLD)
                {
                    velocity = Vector2.Zero;
                }

                velocity *= VISCOSITY;

                float dx = x + delta * velocity.X;
                float dy = y + delta * velocity.Y;

                CalculateMovementFactors(dx, dy,
                    out int floorX, out int ceilX, out int floorY, out int ceilY,
                    out float weightRight, out float weightLeft, out float weightBottom, out float weightTop);

                floorX = floorX.PositiveModulo(PlaneSize);
                ceilX = ceilX.PositiveModulo(PlaneSize);
                floorY = floorY.PositiveModulo(PlaneSize);
                ceilY = ceilY.PositiveModulo(PlaneSize);

                Density[floorX, floorY] += oldDensity * decayRates * weightLeft * weightTop;
                Density[floorX, ceilY] += oldDensity * decayRates * weightLeft * weightBottom;
                Density[ceilX, floorY] += oldDensity * decayRates * weightRight * weightTop;
                Density[ceilX, ceilY] += oldDensity * decayRates * weightRight * weightBottom;
            }
        }
    }

    private void PartialUpdateTextureImage(int x0, int y0, int width, int height)
    {
        for (int x = x0; x < x0 + width; ++x)
        {
            for (int y = y0; y < y0 + height; ++y)
            {
                var pixel = Density[x, y] * (1 / Constants.CLOUD_MAX_INTENSITY_SHOWN);
                image!.SetPixel(x, y, new Color(pixel.X, pixel.Y, pixel.Z, pixel.W));
            }
        }
    }

    private void PartialClearDensity(int x0, int y0, int width, int height)
    {
        for (int x = x0; x < x0 + width; ++x)
        {
            for (int y = y0; y < y0 + height; ++y)
            {
                Density[x, y] = Vector4.Zero;
            }
        }
    }

    private void PartialDiffuseCenter(int x0, int y0, int width, int height, float delta)
    {
        PartialDiffuse(x0 + Constants.CLOUD_PLANE_EDGE_WIDTH / 2, y0 + Constants.CLOUD_PLANE_EDGE_WIDTH / 2, width
            - Constants.CLOUD_PLANE_EDGE_WIDTH, height - Constants.CLOUD_PLANE_EDGE_WIDTH, delta);
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
        for (int i = 0; i < Constants.CLOUDS_IN_ONE; ++i)
        {
            if (Compounds[i] == compound)
                return i;
        }

        return -1;
    }

    private void CreateDensityTexture()
    {
        image = Image.CreateEmpty(PlaneSize, PlaneSize, false, Image.Format.Rgba8);
        texture = ImageTexture.CreateFromImage(image);

        var material = (ShaderMaterial)MaterialOverride;
        material.SetShaderParameter("densities", texture);
    }

    private void SetMaterialUVForPosition()
    {
        var material = (ShaderMaterial)MaterialOverride;

        // No clue how this math ends up with the right UV offsets - hhyyrylainen
        material.SetShaderParameter(uvOffsetParameterName, new Vector2(
            playersPosition.X / (float)Constants.CLOUD_PLANE_SQUARES_PER_SIDE,
            playersPosition.Y / (float)Constants.CLOUD_PLANE_SQUARES_PER_SIDE));
    }
}
