// Toggles using cached to world shift vectors or recalculating them each time

#define CACHE_WORLD_COORDINATES

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using SharedBase.Archive;
using Systems;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;
using Vector4 = System.Numerics.Vector4;

#if CACHE_WORLD_COORDINATES
using System.Collections.Frozen;
#endif

/// <summary>
///   A single compound cloud plane that handles fluid simulation for 4 compound types at a single grid square location
///   (can be repositioned as the player moves)
/// </summary>
public partial class CompoundCloudPlane : MeshInstance3D, ISaveLoadedTracked, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 2;

    /// <summary>
    ///   The current densities of compounds. This uses custom writing, so this is ignored.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Because this is such a high-priority system, this uses a bit more happily null suppressing than elsewhere
    ///   </para>
    /// </remarks>
    public Vector4[] Density = null!;

    public Vector4[] OldDensity = null!;

    public Compound[] Compounds = null!;

    // TODO: give each cloud (compound type) a viscosity value in the JSON file and use it instead.
    private const float VISCOSITY = 0.0525f;

    private readonly StringName brightnessParameterName = new("BrightnessMultiplier");
    private readonly StringName uvOffsetParameterName = new("UVOffset");

#if CACHE_WORLD_COORDINATES
    /// <summary>
    ///   Precalculated cache of world shift vectors. This dictionary contains 81 values.
    ///   And is filled in _Ready once the cloud size is known.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This could be reimplemented as a 256-element flat array, but that actually loses in most tests to the
    ///     frozen dictionary and only in very specific optimizer scenarios it wins. See:
    ///     https://forum.revolutionarygamesstudio.com/t/improving-cloud-performance-with-caching/1232/4
    ///   </para>
    /// </remarks>
    private FrozenDictionary<int, Vector2> cachedWorldShiftVectors = null!;
#endif

    private CompoundDefinition?[] compoundDefinitions = null!;

    private Image? image;
    private ImageTexture texture = null!;
    private FluidCurrentsSystem? fluidSystem;

    private Vector4 decayRates;

    private byte[] tempBuffer = null!;

    /// <summary>
    ///   Which square plane player is in
    /// </summary>
    private Vector2I playersPosition = new(0, 0);

    /// <summary>
    ///   To allow multithreaded operations, a cached world position is needed
    /// </summary>
    private Vector2 cachedWorldPosition;

    public int CloudResolution { get; private set; }

    public int PlaneSize { get; private set; }

    public bool IsLoadedFromSave { get; set; }

    /// <summary>
    ///   This is used in data copy.
    /// </summary>
    public byte[] TempBuffer => tempBuffer;

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
        if (version <= 1)
        {
            var oldPosition = reader.ReadVector3();
            instance.cachedWorldPosition = new Vector2(oldPosition.X, oldPosition.Z);
        }
        else
        {
            instance.cachedWorldPosition = reader.ReadVector2();
        }

        instance.CloudResolution = reader.ReadInt32();
        instance.PlaneSize = reader.ReadInt32();
        instance.Position = reader.ReadVector3();

        // Then the density data
        var buffer = new byte[instance.PlaneSize * 4 * 4];

        int dimensions = instance.PlaneSize;

        var target = new Vector4[dimensions * dimensions];

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

                target[x + y * dimensions] = vector4;
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
                var vector4 = localDensity[x + y * dimensions];

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

            Density = new Vector4[PlaneSize * PlaneSize];
            OldDensity = new Vector4[PlaneSize * PlaneSize];
            ClearContents();
        }
        else
        {
            // Recreate the texture if the size changes
            // TODO: could resample the density data here to allow changing the cloud resolution or size
            // without starting a new save
            CreateDensityTexture();

            OldDensity = new Vector4[PlaneSize * PlaneSize];
            SetMaterialUVForPosition();
        }

#if CACHE_WORLD_COORDINATES
        cachedWorldShiftVectors = PrecalculateWorldShiftVectors();
#endif
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
        cachedWorldPosition = new Vector2(Position.X, Position.Z);

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
    ///   This is not run in parallel.
    /// </summary>
    public void DiffuseEdges(float deltaTime)
    {
        deltaTime *= 100.0f;

        int planeSize = PlaneSize;
        int edgeWidth = Constants.CLOUD_PLANE_EDGE_WIDTH;
        int halfEdgeWidth = edgeWidth / 2;
        int squaresPerSide = Constants.CLOUD_PLANE_SQUARES_PER_SIDE;
        int planeChunkSize = planeSize / squaresPerSide;

        for (int column = 0; column <= squaresPerSide; ++column)
        {
            int boundaryCenter = column * planeChunkSize;
            int horizontalStart = Math.Max(0, boundaryCenter - halfEdgeWidth);
            int horizontalEnd = Math.Min(planeSize, boundaryCenter + halfEdgeWidth);

            AreaDiffuse(horizontalStart, horizontalEnd, 0, planeSize, deltaTime);
        }

        for (int square = 0; square < squaresPerSide; ++square)
        {
            int horizontalStart = square * planeChunkSize + halfEdgeWidth;
            int horizontalEnd = (square + 1) * planeChunkSize - halfEdgeWidth;

            for (int row = 0; row <= squaresPerSide; ++row)
            {
                int boundaryCenter = row * planeChunkSize;
                int verticalStart = Math.Max(0, boundaryCenter - halfEdgeWidth);
                int verticalEnd = Math.Min(planeSize, boundaryCenter + halfEdgeWidth);

                AreaDiffuse(horizontalStart, horizontalEnd, verticalStart, verticalEnd, deltaTime);
            }
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueDiffuseCloud(float deltaTime, List<Task> queue)
    {
        deltaTime *= 100.0f;
        int slices = Constants.CLOUD_PLANE_SQUARES_PER_SIDE * Constants.CLOUD_PLANE_SQUARES_PER_SIDE;

        for (int slice = 0; slice < slices; ++slice)
        {
            int atSlice = slice;
            queue.Add(new Task(() => PartialDiffuse(atSlice, slices, deltaTime)));
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueAdvectCloud(float delta, List<Task> queue)
    {
        delta *= 100.0f;

        int slices = Constants.CLOUD_PLANE_SQUARES_PER_SIDE * Constants.CLOUD_PLANE_SQUARES_PER_SIDE;

        for (int slice = 0; slice < slices; ++slice)
        {
            int atSlice = slice;

            // TODO: fix task allocations
            var task = new Task(() => PartialAdvect(atSlice, slices, delta));
            queue.Add(task);
        }
    }

    public void UpdateTexture()
    {
        int width = image!.GetWidth();
        int height = image.GetHeight();
        int size = width * height * 4;

        image!.SetData(width, height, false, image.GetFormat(), tempBuffer.AsSpan(0, size));
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
    ///   Interlocked add-variant that is thread safe
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
                    seenCurrentAmount = Density[x + y * PlaneSize].X;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x + y * PlaneSize].X, newValue,
                           seenCurrentAmount) != seenCurrentAmount);

                break;
            }

            case 1:
            {
                do
                {
                    seenCurrentAmount = Density[x + y * PlaneSize].Y;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x + y * PlaneSize].Y, newValue,
                           seenCurrentAmount) != seenCurrentAmount);

                break;
            }

            case 2:
            {
                do
                {
                    seenCurrentAmount = Density[x + y * PlaneSize].Z;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x + y * PlaneSize].Z, newValue,
                           seenCurrentAmount) != seenCurrentAmount);

                break;
            }

            case 3:
            {
                do
                {
                    seenCurrentAmount = Density[x + y * PlaneSize].W;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x + y * PlaneSize].W, newValue,
                           seenCurrentAmount) != seenCurrentAmount);

                break;
            }

            default:
                throw new ArgumentException("This cloud doesn't handle the given compound type");
        }

        // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    /// <summary>
    ///   Add-cloud variant that ignores unhandled compound types
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
                    seenCurrentAmount = Density[x + y * PlaneSize].X;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x + y * PlaneSize].X, newValue,
                           seenCurrentAmount) != seenCurrentAmount);

                return true;
            }

            case 1:
            {
                do
                {
                    seenCurrentAmount = Density[x + y * PlaneSize].Y;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x + y * PlaneSize].Y, newValue,
                           seenCurrentAmount) != seenCurrentAmount);

                return true;
            }

            case 2:
            {
                do
                {
                    seenCurrentAmount = Density[x + y * PlaneSize].Z;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x + y * PlaneSize].Z, newValue,
                           seenCurrentAmount) != seenCurrentAmount);

                return true;
            }

            case 3:
            {
                do
                {
                    seenCurrentAmount = Density[x + y * PlaneSize].W;
                    newValue = seenCurrentAmount + density;
                }
                while (Interlocked.CompareExchange(ref Density[x + y * PlaneSize].W, newValue,
                           seenCurrentAmount) != seenCurrentAmount);

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
        float amountInCloud = HackyAddress(ref Density[x + y * PlaneSize], GetCompoundIndex(compound));
        var amountToGive = amountInCloud * fraction;

        if (amountInCloud - amountToGive < 0.1f)
        {
            // Taking basically everything in the cloud
            Density[x + y * PlaneSize] += CalculateCloudToAdd(compound, -amountInCloud);
        }
        else
        {
            Density[x + y * PlaneSize] += CalculateCloudToAdd(compound, -amountToGive);
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
                return Interlocked.CompareExchange(ref Density[x + y * PlaneSize].X, newValue,
                    seenCurrentAmount) == seenCurrentAmount;
            case 1:
                return Interlocked.CompareExchange(ref Density[x + y * PlaneSize].Y, newValue,
                    seenCurrentAmount) == seenCurrentAmount;
            case 2:
                return Interlocked.CompareExchange(ref Density[x + y * PlaneSize].Z, newValue,
                    seenCurrentAmount) == seenCurrentAmount;
            case 3:
                return Interlocked.CompareExchange(ref Density[x + y * PlaneSize].W, newValue,
                    seenCurrentAmount) == seenCurrentAmount;
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
        float amountInCloud = HackyAddress(ref Density[x + y * PlaneSize], GetCompoundIndex(compound));
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

            float amount = HackyAddress(ref Density[x + y * PlaneSize], i);
            if (amount > 0)
                result[compound] = amount;
        }
    }

    /// <summary>
    ///   Checks if the position is in this cloud, also returns relative coordinates
    /// </summary>
    public bool ContainsPosition(Vector3 worldPosition, out int x, out int y)
    {
        ConvertToCloudLocal(worldPosition, out x, out y);
        return x >= 0 && y >= 0 && x < PlaneSize && y < PlaneSize;
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
            worldPosition.Z + radius < cachedWorldPosition.Y - Constants.CLOUD_SIZE ||
            worldPosition.Z - radius >= cachedWorldPosition.Y + Constants.CLOUD_SIZE)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///   Converts world coordinate to cloud-relative (top left) coordinates
    /// </summary>
    public void ConvertToCloudLocal(Vector3 worldPosition, out int x, out int y)
    {
        var topLeftRelative = new Vector2(worldPosition.X, worldPosition.Z) - cachedWorldPosition;

        // Floor is used here because otherwise the last coordinate is wrong
        x = ((int)Math.Floor((topLeftRelative.X + Constants.CLOUD_SIZE) / CloudResolution)
            + playersPosition.X * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE) % PlaneSize;
        y = ((int)Math.Floor((topLeftRelative.Y + Constants.CLOUD_SIZE) / CloudResolution)
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
            Constants.CLOUD_SIZE + cachedWorldPosition.X,
            0,
            cloudY * CloudResolution + ((4 - playersPosition.Y) % 3 - 1) *
            CloudResolution * PlaneSize /
            Constants.CLOUD_PLANE_SQUARES_PER_SIDE -
            Constants.CLOUD_SIZE + cachedWorldPosition.Y);

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

            // Skip if the compound is non-useful or disallowed to be absorbed
            if (!compoundDefinitions[i]!.IsAbsorbable
                || (!storage.IsUseful(compound) && !compoundDefinitions[i]!.AlwaysAbsorbable))
            {
                continue;
            }

            // Loop here to retry in case we read stale data
            while (true)
            {
                // Overestimate of how many compounds we get
                float cloudAmount = HackyAddress(ref Density[localX + localY * PlaneSize], i);
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

                    // Allow partial absorption to allow cells to take from high-density clouds
                    multiplier = freeSpace / generousAmount;
                }

                if (!TakeCompoundInterlocked(i, localX, localY, fractionToTake * multiplier, cloudAmount,
                        out float taken))
                {
                    // Value was updated since we read it, we need to retry
                    continue;
                }

                taken *= Constants.ABSORPTION_RATIO;

                // This should never fail to add the full amount of compound as we checked the free space above and
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetWorldShiftKey(int xSquare, int ySquare, int playerX, int playerY)
    {
        // Each value is in the 0-2 range, so two bits per value is enough to avoid collisions.
        return xSquare | ySquare << 2 | playerX << 4 | playerY << 6;
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

        weightRight = dx - floorX;
        weightLeft = 1.0f - weightRight;
        weightBottom = dy - floorY;
        weightTop = 1.0f - weightBottom;
    }

    private Vector4 CalculateCloudToAdd(Compound compound, float density)
    {
        return new Vector4(Compounds[0] == compound ? density : 0.0f,
            Compounds[1] == compound ? density : 0.0f,
            Compounds[2] == compound ? density : 0.0f,
            Compounds[3] == compound ? density : 0.0f);
    }

    private void PartialDiffuse(int slice, int slices, float delta)
    {
        int planeSize = PlaneSize;
        int horizontalStart = slice * planeSize / slices;
        int horizontalEnd = (slice + 1) * planeSize / slices;

        float diffusionAmount = delta * Constants.CLOUD_DIFFUSION_RATE;
        float neighborWeight = diffusionAmount * 0.25f;
        float centerWeight = 1.0f - diffusionAmount;

        ReadOnlySpan<Vector4> sourceDensity = Density.AsSpan();
        Span<Vector4> destinationDensity = OldDensity.AsSpan();
        ReadOnlySpan<float> sourceFloats = MemoryMarshal.Cast<Vector4, float>(sourceDensity);
        Span<float> destinationFloats = MemoryMarshal.Cast<Vector4, float>(destinationDensity);

        var centerWeightVector = Vector256.Create(centerWeight);
        var neighborWeightVector = Vector256.Create(neighborWeight);

        ref float sourceReference = ref MemoryMarshal.GetReference(sourceFloats);
        ref float destinationReference = ref MemoryMarshal.GetReference(destinationFloats);

        bool avx2Supported = Avx2.IsSupported;

        for (int horizontalIndex = horizontalStart; horizontalIndex < horizontalEnd; ++horizontalIndex)
        {
            int currentRowOffset = horizontalIndex * planeSize;
            int previousRowOffset = (horizontalIndex == 0 ? planeSize - 1 : horizontalIndex - 1) * planeSize;
            int nextRowOffset = (horizontalIndex == planeSize - 1 ? 0 : horizontalIndex + 1) * planeSize;

            int firstIndex = currentRowOffset;
            destinationDensity[firstIndex] = sourceDensity[firstIndex] * centerWeight +
                (sourceDensity[currentRowOffset + (planeSize - 1)] + sourceDensity[currentRowOffset + 1] +
                    sourceDensity[previousRowOffset] + sourceDensity[nextRowOffset]) * neighborWeight;

            int verticalIndex = 1;
            int safeLimit = planeSize - 1;

            if (avx2Supported)
            {
                // Use Avx2 SIMD to vectorise diffusion.

                // Most of the operations are now vectorised, except for a possible final tail that doesn't fit in a
                // Vector256. This is taken care of after the current conditional branch.
                for (; verticalIndex <= safeLimit - 2; verticalIndex += 2)
                {
                    uint offset = (uint)(currentRowOffset + verticalIndex) << 2;

                    var center = Vector256.LoadUnsafe(ref sourceReference, offset);

                    var up = Vector256.LoadUnsafe(ref sourceReference, offset - 4);
                    var down = Vector256.LoadUnsafe(ref sourceReference, offset + 4);
                    var left = Vector256.LoadUnsafe(ref sourceReference, (uint)(previousRowOffset +
                        verticalIndex) << 2);
                    var right = Vector256.LoadUnsafe(ref sourceReference, (uint)(nextRowOffset + verticalIndex) << 2);

                    var neighbors = Avx.Add(Avx.Add(up, down), Avx.Add(left, right));

                    var result = Avx.Add(
                        Avx.Multiply(center, centerWeightVector),
                        Avx.Multiply(neighbors, neighborWeightVector));

                    result.StoreUnsafe(ref destinationReference, offset);
                }
            }

            // If Avx2 is unsupported, the following loops will take care of the scalar operations. That must be
            // executed after the SIMD operations if Avx2 is supported anyway, as we need to take care of a possible
            // "tail" if the PlaneSize is not aligned to the previous SIMD algorithm.

            // This is the scalar algorithm. It executes if Avx2 is not supported and on the tail we discussed in the
            // previous comments.
            for (; verticalIndex < safeLimit; ++verticalIndex)
            {
                int currentIndex = currentRowOffset + verticalIndex;
                destinationDensity[currentIndex] = sourceDensity[currentIndex] * centerWeight +
                    (sourceDensity[currentIndex - 1] + sourceDensity[currentIndex + 1] +
                        sourceDensity[previousRowOffset + verticalIndex] + sourceDensity[nextRowOffset + verticalIndex])
                    * neighborWeight;
            }

            if (verticalIndex < planeSize)
            {
                int lastIndex = currentRowOffset + verticalIndex;
                destinationDensity[lastIndex] = sourceDensity[lastIndex] * centerWeight +
                    (sourceDensity[lastIndex - 1] + sourceDensity[currentRowOffset] +
                        sourceDensity[previousRowOffset + verticalIndex] + sourceDensity[nextRowOffset + verticalIndex])
                    * neighborWeight;
            }
        }
    }

    private void AreaDiffuse(int horizontalStart, int horizontalEnd, int verticalStart, int verticalEnd,
        float delta)
    {
        int planeSize = PlaneSize;
        float diffusionAmount = delta * Constants.CLOUD_DIFFUSION_RATE;
        float neighborWeight = diffusionAmount * 0.25f;
        float centerWeight = 1.0f - diffusionAmount;

        var sourceDensity = Density.AsSpan();
        var destinationDensity = OldDensity.AsSpan();

        for (int horizontalIndex = horizontalStart; horizontalIndex < horizontalEnd; ++horizontalIndex)
        {
            int currentRowOffset = horizontalIndex * planeSize;
            int previousRowOffset = (horizontalIndex == 0 ? planeSize - 1 : horizontalIndex - 1) * planeSize;
            int nextRowOffset = (horizontalIndex == planeSize - 1 ? 0 : horizontalIndex + 1) * planeSize;

            int verticalIndex = verticalStart;

            if (verticalIndex == 0 && verticalIndex < verticalEnd)
            {
                int currentIndex = currentRowOffset + 0;
                destinationDensity[currentIndex] = sourceDensity[currentIndex] * centerWeight +
                    (sourceDensity[currentRowOffset + (planeSize - 1)] + sourceDensity[currentRowOffset + 1] +
                        sourceDensity[previousRowOffset] + sourceDensity[nextRowOffset]) * neighborWeight;
                ++verticalIndex;
            }

            int safeLimit = Math.Min(verticalEnd, planeSize - 1);
            for (; verticalIndex < safeLimit; ++verticalIndex)
            {
                int currentIndex = currentRowOffset + verticalIndex;
                destinationDensity[currentIndex] = sourceDensity[currentIndex] * centerWeight +
                    (sourceDensity[currentIndex - 1] + sourceDensity[currentIndex + 1] +
                        sourceDensity[previousRowOffset + verticalIndex] + sourceDensity[nextRowOffset + verticalIndex])
                    * neighborWeight;
            }

            if (verticalIndex == planeSize - 1 && verticalIndex < verticalEnd)
            {
                int currentIndex = currentRowOffset + verticalIndex;
                destinationDensity[currentIndex] = sourceDensity[currentIndex] * centerWeight +
                    (sourceDensity[currentIndex - 1] + sourceDensity[currentRowOffset] +
                        sourceDensity[previousRowOffset + verticalIndex] + sourceDensity[nextRowOffset + verticalIndex])
                    * neighborWeight;
            }
        }
    }

#if CACHE_WORLD_COORDINATES
    private FrozenDictionary<int, Vector2> PrecalculateWorldShiftVectors()
    {
        var shiftCache = new Dictionary<int, Vector2>(81);
        int planeChunkSize = PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE;
        int worldShift = planeChunkSize * CloudResolution;

        int[] planeSquares = { 0, 1, 2 };
        int[] playerPositions = { 0, 1, 2 };

        foreach (int xSquare in planeSquares)
        {
            foreach (int ySquare in planeSquares)
            {
                foreach (int playerX in playerPositions)
                {
                    foreach (int playerY in playerPositions)
                    {
                        // When not caching equivalent math is in GetWorldPositionForAdvection
                        int x0 = xSquare * planeChunkSize;
                        int y0 = ySquare * planeChunkSize;
                        int xShift = GetEdgeShift(x0, playerX);
                        int yShift = GetEdgeShift(y0, playerY);

                        var wholePlaneShift = new Vector2(worldShift * ((4 - playerX) % 3 - 1) - Constants.CLOUD_SIZE,
                            worldShift * ((4 - playerY) % 3 - 1) - Constants.CLOUD_SIZE);

                        var edgePlanesShift = new Vector2(xShift * worldShift, yShift * worldShift);

                        int key = GetWorldShiftKey(xSquare, ySquare, playerX, playerY);
                        shiftCache[key] = wholePlaneShift + edgePlanesShift;
                    }
                }
            }
        }

        if (shiftCache.Count != 81)
            throw new Exception("Logic error in PrecalculateWorldShiftVectors");

        return shiftCache.ToFrozenDictionary();
    }
#endif

    private Vector2 GetWorldPositionForAdvection(int x0, int y0)
    {
#if CACHE_WORLD_COORDINATES
        int planeChunkSize = PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE;
        var key = GetWorldShiftKey(x0 / planeChunkSize, y0 / planeChunkSize, playersPosition.X, playersPosition.Y);

        // In benchmarks the null ref or direct try get basically both get wins and losses, but the TryGet wins
        // slightly more often, so it is used
        /*ref readonly var cached = ref cachedWorldShiftVectors.GetValueRefOrNullRef(key);
        if (!Unsafe.IsNullRef(in cached))
            return cachedWorldPosition + cached;*/

        if (cachedWorldShiftVectors.TryGetValue(key, out var cached))
            return cachedWorldPosition + cached;

#if DEBUG
        throw new ArgumentException("Position is impossible for cloud world shift lookup");
#else
        return cachedWorldPosition;
#endif
#else
        // Same math as in PrecalculateWorldShiftVectors. This is used when not caching.
        int planeChunkSize = PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE;
        int worldShift = planeChunkSize * CloudResolution;
        var playerX = playersPosition.X;
        var playerY = playersPosition.Y;

        int xShift = GetEdgeShift(x0, playerX);
        int yShift = GetEdgeShift(y0, playerY);

        var wholePlaneShift = new Vector2(worldShift * ((4 - playerX) % 3 - 1) - Constants.CLOUD_SIZE,
            worldShift * ((4 - playerY) % 3 - 1) - Constants.CLOUD_SIZE);

        var edgePlanesShift = new Vector2(xShift * worldShift, yShift * worldShift);

        return cachedWorldPosition + wholePlaneShift + edgePlanesShift;
#endif
    }

    private int GetEdgeShift(int coord, int playerPos)
    {
        if (coord == 0 && playerPos == 1)
            return 3;
        if (coord == 2 * PlaneSize / Constants.CLOUD_PLANE_SQUARES_PER_SIDE && playerPos == 2)
            return -3;

        return 0;
    }

    private void PartialAdvect(int slice, int slices, float delta)
    {
        int planeSize = PlaneSize;
        int rowStart = slice * planeSize / slices;
        int rowEnd = (slice + 1) * planeSize / slices;
        int rowCount = rowEnd - rowStart;

        var source = OldDensity.AsSpan(rowStart * planeSize, rowCount * planeSize);
        var destination = Density.AsSpan();
        var bufferSpan = tempBuffer.AsSpan(rowStart * planeSize * 4, rowCount * planeSize * 4);

        float resolution = CloudResolution;
        Vector2 worldPositionBase = GetWorldPositionForAdvection(0, rowStart);

        const float intensityScale = 255.0f / Constants.CLOUD_MAX_INTENSITY_SHOWN;

        int bufferIndex = 0;
        for (int y = 0; y < rowCount; ++y)
        {
            int rowOffset = y * planeSize;
            float worldY = worldPositionBase.Y + y * resolution;
            int absoluteY = y + rowStart;

            for (int x = 0; x < planeSize; ++x)
            {
                Vector4 currentDensity = source[rowOffset + x];

                bufferSpan[bufferIndex] = (byte)Math.Clamp(currentDensity.X * intensityScale, 0, 255);
                bufferSpan[bufferIndex + 1] = (byte)Math.Clamp(currentDensity.Y * intensityScale, 0, 255);
                bufferSpan[bufferIndex + 2] = (byte)Math.Clamp(currentDensity.Z * intensityScale, 0, 255);
                bufferSpan[bufferIndex + 3] = (byte)Math.Clamp(currentDensity.W * intensityScale, 0, 255);
                bufferIndex += 4;

                if (currentDensity.X + currentDensity.Y + currentDensity.Z + currentDensity.W < 1.0f)
                    continue;

                float worldX = worldPositionBase.X + x * resolution;
                Vector2 velocity = fluidSystem!.VelocityAt(new Vector2(worldX, worldY));

                if (MathF.Abs(velocity.X) + MathF.Abs(velocity.Y) < Constants.CURRENT_COMPOUND_CLOUD_ADVECT_THRESHOLD)
                    velocity = Vector2.Zero;

                velocity *= VISCOSITY;

                float targetX = x + delta * velocity.X;
                float targetY = absoluteY + delta * velocity.Y;

                CalculateMovementFactors(targetX, targetY,
                    out int floorX, out int ceilingX, out int floorY, out int ceilingY,
                    out float weightRight, out float weightLeft, out float weightBottom, out float weightTop);

                if ((uint)floorX >= (uint)planeSize)
                    floorX = (floorX < 0) ? floorX + planeSize : floorX - planeSize;
                if ((uint)ceilingX >= (uint)planeSize)
                    ceilingX = (ceilingX < 0) ? ceilingX + planeSize : ceilingX - planeSize;
                if ((uint)floorY >= (uint)planeSize)
                    floorY = (floorY < 0) ? floorY + planeSize : floorY - planeSize;
                if ((uint)ceilingY >= (uint)planeSize)
                    ceilingY = (ceilingY < 0) ? ceilingY + planeSize : ceilingY - planeSize;

                int floorYOffset = floorY * planeSize;
                int ceilingYOffset = ceilingY * planeSize;

                Vector4 decayed = currentDensity * decayRates;
                Vector4 decayedLeft = decayed * weightLeft;
                Vector4 decayedRight = decayed * weightRight;

                destination[floorX + floorYOffset] += decayedLeft * weightTop;
                destination[floorX + ceilingYOffset] += decayedLeft * weightBottom;
                destination[ceilingX + floorYOffset] += decayedRight * weightTop;
                destination[ceilingX + ceilingYOffset] += decayedRight * weightBottom;
            }
        }
    }

    private void PartialClearDensity(int x0, int y0, int width, int height)
    {
        for (int y = x0; y < x0 + width; ++y)
        {
            for (int x = y0; x < y0 + height; ++x)
            {
                Density[x + y * PlaneSize] = Vector4.Zero;
            }
        }
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
        int requestedSize = PlaneSize * PlaneSize * 4;
        if (tempBuffer == null! || requestedSize > tempBuffer.Length)
        {
            tempBuffer = new byte[requestedSize];
        }

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
