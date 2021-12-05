using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

[SceneLoadedClass("res://src/microbe_stage/CompoundCloudPlane.tscn", UsesEarlyResolve = false)]
public class CompoundCloudPlane : CSGMesh, ISaveLoadedTracked
{
    /// <summary>
    ///   The current densities of compounds. This uses custom writing so this is ignored
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     A cloud is designed as a Size * Size grid of Vector4 densities, a grid relative to the cloud position.
    ///     Each density of the Vector4 is the density of a compound, in the order recorder in Compounds array below.
    ///     TODO: Investigate why it should loop on edges
    ///   </para>
    /// </remarks>
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

    /// <summary>
    ///   The reference position of the cloud in terms of square subdivisions.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The coordinates are comprised within 0 and (Constants.CLOUD_SQUARES_PER_SIDE - 1)
    ///   </para>
    /// </remarks>
    [JsonProperty]
    private Int2 position = new Int2(0, 0);

    [JsonProperty]
    public int Resolution { get; private set; }

    /// <summary>
    ///   The size, as a number of grid cells, of the cloud.
    /// </summary>
    [JsonProperty]
    public int Size { get; private set; }

    /// <summary>
    ///   The size, as a number of grid cells, of the big square subdivisions of the cloud.
    /// </summary>
    public int SquaresSize => Size / Constants.CLOUD_SQUARES_PER_SIDE;

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

    public void UpdatePosition(Int2 newPosition)
    {
        var newX = newPosition.x.PositiveModulo(Constants.CLOUD_SQUARES_PER_SIDE);
        var newY = newPosition.y.PositiveModulo(Constants.CLOUD_SQUARES_PER_SIDE);

        // TODO, Factor squares per size in rect
        // Todo use rotate ?
        // TODO investigate one var + positivemods.
        if (newX == (position.x + 1) % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            var currentVerticalSlice = new IntRect(position.x * SquaresSize, 0,
                SquaresSize, Size);
            PartialClearDensity(currentVerticalSlice);
        }
        else if (newX == (position.x - 1).PositiveModulo(Constants.CLOUD_SQUARES_PER_SIDE))
        {
            var previousVerticalSlice = new IntRect(newX * SquaresSize, 0,
                SquaresSize, Size);
            PartialClearDensity(previousVerticalSlice);
        }

        if (newY == (position.y + 1) % Constants.CLOUD_SQUARES_PER_SIDE)
        {
            var currentHorizontalSlice = new IntRect(0, position.y * SquaresSize,
                Size, SquaresSize);
            PartialClearDensity(currentHorizontalSlice);
        }
        else if (newY == (position.y - 1).PositiveModulo(Constants.CLOUD_SQUARES_PER_SIDE))
        {
            var previousHorizontalSlice = new IntRect(0, newX * SquaresSize,
                Size, SquaresSize);
            PartialClearDensity(previousHorizontalSlice);
        }

        position = new Int2(newX, newY);

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
    /// <remarks>
    ///   This has a structure very similar to UpdateEdgesAfterCenter below.
    ///   TODO: Possible refactory?
    /// </remarks>
    public void UpdateEdgesBeforeCenter(float delta)
    {
        if (position.x != 0)
        {
            var rectangle = new IntRect(0, 0,
                Constants.CLOUD_EDGE_WIDTH, Size);
            PartialDiffuse(rectangle, delta, true);

            rectangle.X += Size - rectangle.Width;
            PartialDiffuse(rectangle, delta, true);
        }

        if (position.x != 1)
        {
            var rectangle = new IntRect(SquaresSize - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size);
            PartialDiffuse(rectangle, delta, true);
        }

        if (position.x != 2)
        {
            var rectangle = new IntRect(2 * SquaresSize - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size);
            PartialDiffuse(rectangle, delta, true);
        }

        if (position.y != 0)
        {
            var rectangle = new IntRect(Constants.CLOUD_EDGE_WIDTH / 2, 0,
                SquaresSize - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2);

            PartialDiffuse(rectangle, delta, true);
            rectangle.X += SquaresSize;
            PartialDiffuse(rectangle, delta, true);
            rectangle.X += SquaresSize;
            PartialDiffuse(rectangle, delta, true);

            rectangle.Y += Size - Constants.CLOUD_EDGE_WIDTH / 2;

            PartialDiffuse(rectangle, delta, true);
            rectangle.X -= SquaresSize;
            PartialDiffuse(rectangle, delta, true);
            rectangle.X -= SquaresSize;
            PartialDiffuse(rectangle, delta, true);
        }

        if (position.y != 1)
        {
            var rectangle = new IntRect(Constants.CLOUD_EDGE_WIDTH / 2,
                SquaresSize - Constants.CLOUD_EDGE_WIDTH / 2,
                SquaresSize - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH);

            PartialDiffuse(rectangle, delta, true);
            rectangle.X += SquaresSize;
            PartialDiffuse(rectangle, delta, true);
            rectangle.X += SquaresSize;
            PartialDiffuse(rectangle, delta, true);
        }

        if (position.y != 2)
        {
            var rectangle = new IntRect(
                    Constants.CLOUD_EDGE_WIDTH / 2,
                    2 * SquaresSize - Constants.CLOUD_EDGE_WIDTH / 2,
                    SquaresSize - Constants.CLOUD_EDGE_WIDTH,
                    Constants.CLOUD_EDGE_WIDTH);

            PartialDiffuse(rectangle, delta, true);
            rectangle.X += SquaresSize;
            PartialDiffuse(rectangle, delta, true);
            rectangle.X += SquaresSize;
            PartialDiffuse(rectangle, delta, true);
        }
    }

    /// <summary>
    ///   Updates the edge concentrations of this cloud after the rest of the cloud.
    ///   This is not ran in parallel.
    /// </summary>
    public void UpdateEdgesAfterCenter(float delta)
    {
        var pos = new Vector2(Translation.x, Translation.z);

        if (position.x != 0)
        {
            var rectangle = new IntRect(0, 0, Constants.CLOUD_EDGE_WIDTH / 2, Size);
            PartialAdvect(rectangle, delta, pos, true);

            rectangle.X += Size - rectangle.Width;
            PartialAdvect(rectangle, delta, pos, true);
        }

        if (position.x != 1)
        {
            var rectangle = new IntRect(SquaresSize - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size);
            PartialAdvect(rectangle, delta, pos, true);
        }

        if (position.x != 2)
        {
            var rectangle = new IntRect(2 * SquaresSize - Constants.CLOUD_EDGE_WIDTH / 2, 0,
                Constants.CLOUD_EDGE_WIDTH, Size);
            PartialAdvect(rectangle, delta, pos, true);
        }

        if (position.y != 0)
        {
            var rectangle = new IntRect(Constants.CLOUD_EDGE_WIDTH / 2, 0,
                SquaresSize - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH / 2);

            PartialAdvect(rectangle, delta, pos, true);
            rectangle.X += SquaresSize;
            PartialAdvect(rectangle, delta, pos, true);
            rectangle.X += SquaresSize;
            PartialAdvect(rectangle, delta, pos, true);

            rectangle.Y += Size - rectangle.Height;

            // NOTE here we swapped order from original but this should not be an issue.
            // TODO check
            PartialAdvect(rectangle, delta, pos, true);
            rectangle.X -= SquaresSize;
            PartialAdvect(rectangle, delta, pos, true);
            rectangle.X -= SquaresSize;
            PartialAdvect(rectangle, delta, pos, true);
        }

        if (position.y != 1)
        {
            var rectangle = new IntRect(Constants.CLOUD_EDGE_WIDTH / 2,
                SquaresSize - Constants.CLOUD_EDGE_WIDTH / 2,
                SquaresSize - Constants.CLOUD_EDGE_WIDTH,
                Constants.CLOUD_EDGE_WIDTH);

            PartialAdvect(rectangle, delta, pos, true);
            rectangle.X += SquaresSize;
            PartialAdvect(rectangle, delta, pos, true);
            rectangle.X += SquaresSize;
            PartialAdvect(rectangle, delta, pos, true);
        }

        if (position.y != 2)
        {
            var rectangle = new IntRect(
                    Constants.CLOUD_EDGE_WIDTH / 2,
                    2 * SquaresSize - Constants.CLOUD_EDGE_WIDTH / 2,
                    SquaresSize - Constants.CLOUD_EDGE_WIDTH,
                    Constants.CLOUD_EDGE_WIDTH);

            PartialAdvect(rectangle, delta, pos, true);
            rectangle.X += SquaresSize;
            PartialAdvect(rectangle, delta, pos, true);
            rectangle.X += SquaresSize;
            PartialAdvect(rectangle, delta, pos, true);
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueUpdateCloud(float delta, List<Task> queue)
    {
        var pos = new Vector2(Translation.x, Translation.z);

        var cloud = new IntRect(0, 0, Size, Size);
        foreach (var targetSquare in cloud.GetSubdivisionEnumerator(SquaresSize))
        {
            var task = new Task(() => PartialUpdateCenter(targetSquare, delta, pos));
            queue.Add(task);
        }
    }

    /// <summary>
    ///   Updates the cloud in parallel.
    /// </summary>
    public void QueueUpdateTextureImage(List<Task> queue)
    {
        image.Lock();

        var cloud = new IntRect(0, 0, Size, Size);
        foreach (var targetSquare in cloud.GetSubdivisionEnumerator(SquaresSize))
        {
            var task = new Task(() => PartialUpdateTextureImage(targetSquare));
            queue.Add(task);
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
        float amountInCloud = HackyAddress(Density[x, y], GetCompoundIndex(compound));
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
        float amountInCloud = HackyAddress(Density[x, y], GetCompoundIndex(compound));
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

            float amount = HackyAddress(Density[x, y], i);
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
            + position.x * SquaresSize) % Size;
        y = ((int)Math.Floor((topLeftRelative.z + Constants.CLOUD_HEIGHT) / Resolution)
            + position.y * SquaresSize) % Size;
    }

    /// <summary>
    ///   Converts cloud local coordinates to world coordinates
    /// </summary>
    public Vector3 ConvertToWorld(int cloudX, int cloudY)
    {
        return new Vector3(
            cloudX * Resolution + ((4 - position.x) % 3 - 1) * Resolution * SquaresSize -
            Constants.CLOUD_WIDTH,
            0,
            cloudY * Resolution + ((4 - position.y) % 3 - 1) * Resolution * SquaresSize -
            Constants.CLOUD_HEIGHT) + Translation;
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

            // Skip if compound is non-useful
            if (!storage.IsUseful(Compounds[i]))
                continue;

            // Overestimate of how much compounds we get
            float generousAmount = HackyAddress(Density[localX, localY], i) *
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
        var cloud = new IntRect(0, 0, Size, Size);
        foreach (var point in cloud.GetPointEnumerator())
        {
            Density[point.x, point.y] = Vector4.Zero;
            OldDensity[point.x, point.y] = Vector4.Zero;
        }
    }

    /// <summary>
    ///   Calculates the multipliers for the old density to move to new locations
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The name might not be super accurate as I just picked something to reduce code duplication
    ///   </para>
    ///   <para>
    ///     As most parameters are obtainable by +-1, I doubt it is useful to output them so much...
    ///   </para>
    /// </remarks>
    private static void CalculateMovementFactors(float advectedX, float advectedY, out int advectedXFloored, out int advectedXCeiled, out int advectedYFloored, out int advectedYCaped,
        out float xFloorCorrection, out float xCeilingCorrection, out float yFloorCorrection, out float yCeilingCorrection)
    {
        advectedXFloored = (int)Math.Floor(advectedX);
        advectedXCeiled = advectedXFloored + 1;
        advectedYFloored = (int)Math.Floor(advectedY);
        advectedYCaped = advectedYFloored + 1;

        // advectedXFloored is always smaller than advectedX,
        // as both (int) and Math.Floor() return a smaller (or equal number)
        xFloorCorrection = advectedX - advectedXFloored;
        xCeilingCorrection = 1.0f - xFloorCorrection;

        // Same here
        yFloorCorrection = advectedY - advectedYFloored;
        yCeilingCorrection = 1.0f - yFloorCorrection;
    }

    /// <remarks>
    ///   <para>
    ///     Merge the two functions
    ///   </para>
    /// </remarks>
    private void PartialDiffuse(IntRect affectedRectangle, float delta, bool isEdge)
    {
        float diffusion = delta * Constants.CLOUD_DIFFUSION_RATE;

        foreach (var point in affectedRectangle.GetPointEnumerator())
        {
            int adjacentClouds = 4;
            OldDensity[point.x, point.y] =
                Density[point.x, point.y] * (1 - diffusion) +
                GetNeighbouringDensities(point.x, point.y, isEdge) * (diffusion / adjacentClouds);
        }
    }

    private Vector4 GetNeighbouringDensities(int x, int y, bool loopOnEdges)
    {
        if (loopOnEdges)
        {
            return Density[x, (y - 1).PositiveModulo(Size)] +
               Density[x, (y + 1) % Size] +
               Density[(x - 1).PositiveModulo(Size), y] +
               Density[(x + 1) % Size, y];
        }

        return Density[x, y - 1] +
                Density[x, y + 1] +
                Density[x - 1, y] +
                Density[x + 1, y];
    }

    /// <summary>
    ///  Advects a part of a cloud as a rectangular area.
    /// </summary>
    /// <param name="delta">The time step for advection.</param>
    /// <param name="pos"> The absolute position of the cloud in the world.</param>
    /// <remarks>
    ///   <para>
    ///     Using pos as a parameter seems rather weird as this is the method of the cloud...
    ///     TODO: check for refactor.
    ///   </para>
    /// </remarks>
    private void PartialAdvect(IntRect affectedRectangle, float delta, Vector2 pos, bool isCenter)
    {
        foreach (var point in affectedRectangle.GetPointEnumerator())
        {
            if (OldDensity[point.x, point.y].LengthSquared() > 1)
                {
                var velocity = fluidSystem.VelocityAt(
                    pos + (new Vector2(point.x, point.y) * Resolution)) * VISCOSITY;

                // This is ran in parallel, this may not touch the other compound clouds
                float advectedX = point.x + (delta * velocity.x);
                float advectedY = point.y + (delta * velocity.y);

                // So this is clamped to not go to the other clouds
                // TODO CHECK THAT IT WONT GO OVERBOARD
                advectedX = advectedX.Clamp(affectedRectangle.X - 0.5f, affectedRectangle.EndX + 0.5f);
                advectedY = advectedY.Clamp(affectedRectangle.Y - 0.5f, affectedRectangle.EndY + 0.5f);

                CalculateMovementFactors(advectedX, advectedY,
                    out var q0, out var q1, out var r0, out var r1,
                    out var s1, out var s0, out var t1, out var t0);

                if (isCenter)
                {
                    if (IsARelativeCoordinate(q0))
                    {
                        if (IsARelativeCoordinate(r0))
                            Density[q0, r0] += OldDensity[point.x, point.y] * s0 * t0;
                        if (IsARelativeCoordinate(r1))
                            Density[q0, r1] += OldDensity[point.x, point.y] * s0 * t1;
                    }

                    if (IsARelativeCoordinate(q1))
                    {
                        if (IsARelativeCoordinate(r0))
                            Density[q1, r0] += OldDensity[point.x, point.y] * s1 * t0;
                        if (IsARelativeCoordinate(r1))
                            Density[q1, r1] += OldDensity[point.x, point.y] * s1 * t1;
                    }
                }
                else
                {
                    q0 = q0.PositiveModulo(Size);
                    q1 = q1.PositiveModulo(Size);
                    r0 = r0.PositiveModulo(Size);
                    r1 = r1.PositiveModulo(Size);

                    Density[q0, r0] += OldDensity[point.x, point.y] * s0 * t0;
                    Density[q0, r1] += OldDensity[point.x, point.y] * s0 * t1;
                    Density[q1, r0] += OldDensity[point.x, point.y] * s1 * t0;
                    Density[q1, r1] += OldDensity[point.x, point.y] * s1 * t1;
                }
            }
        }
    }

    private bool IsARelativeCoordinate(int coordinate)
    {
        return coordinate >= 0 && coordinate < Size;
    }

    private void PartialUpdateTextureImage(IntRect affectedRectangle)
    {
        foreach (var point in affectedRectangle.GetPointEnumerator())
        {
            var pixel = Density[point.x, point.y] * (1 / Constants.CLOUD_MAX_INTENSITY_SHOWN);
            image.SetPixel(point.x, point.y, new Color(pixel.X, pixel.Y, pixel.Z, pixel.W));
        }
    }

    private void PartialClearDensity(IntRect affectedRectangle)
    {
        foreach (var point in affectedRectangle.GetPointEnumerator())
        {
            Density[point.x, point.y] = Vector4.Zero;
        }
    }

    private void PartialUpdateCenter(IntRect centerRectangle, float delta, Vector2 pos)
    {
        // TODO find name
        var xxxRectangle = centerRectangle.CreateSubRectangle(Constants.CLOUD_EDGE_WIDTH);

        PartialDiffuse(xxxRectangle, delta, false);
        PartialClearDensity(centerRectangle);
        PartialAdvect(xxxRectangle, delta, pos, false);
    }

    private float HackyAddress(Vector4 vector, int index)
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

        // No clue how this math ends up with the right UV offsets - hhyyrylainen
        material.SetShaderParam("UVOffset", new Vector2(position.x / (float)Constants.CLOUD_SQUARES_PER_SIDE,
            position.y / (float)Constants.CLOUD_SQUARES_PER_SIDE));
    }
}
