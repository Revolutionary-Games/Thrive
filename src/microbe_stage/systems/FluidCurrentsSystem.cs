namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using World = DefaultEcs.World;

/// <summary>
///   Gives a push from currents in a fluid to physics entities (that have <see cref="ManualPhysicsControl"/>).
///   Only acts on entities marked with <see cref="CurrentAffected"/>.
/// </summary>
[With(typeof(CurrentAffected))]
[With(typeof(Physics))]
[With(typeof(ManualPhysicsControl))]
[With(typeof(WorldPosition))]
[ReadsComponent(typeof(CurrentAffected))]
[ReadsComponent(typeof(Physics))]
[ReadsComponent(typeof(WorldPosition))]
[RuntimeCost(8)]
[JsonObject(MemberSerialization.OptIn)]
[RunsOnMainThread]
public sealed class FluidCurrentsSystem : AEntitySetSystem<float>
{
    // The following constants should be the same as in CurrentsParticles.gdshader
    private const float DISTURBANCE_TIMESCALE = 1.000f;
    private const float CURRENTS_TIMESCALE = 1.000f / 500.0f;
    private const float CURRENTS_STRETCHING_MULTIPLIER = 1.0f / 10.0f;
    private const float MIN_CURRENT_INTENSITY = 0.4f;
    private const float DISTURBANCE_TO_CURRENTS_RATIO = 0.15f;
    private const float POSITION_SCALING = 0.9f;

    private readonly NoiseTexture3D noiseDisturbancesX;
    private readonly NoiseTexture3D noiseDisturbancesY;
    private readonly NoiseTexture3D noiseCurrentsX;
    private readonly NoiseTexture3D noiseCurrentsY;

    private Image[]? noiseDisturbancesXImage;
    private Image[]? noiseDisturbancesYImage;
    private Image[]? noiseCurrentsXImage;
    private Image[]? noiseCurrentsYImage;

    private GameWorld? gameWorld;

    private float speed = 0.0f;
    private float chaoticness = 0.0f;
    private float scale = 0.0f;

    [JsonProperty]
    private float currentsTimePassed;

    private int noiseWidth = -1;
    private int noiseHeight = -1;

    public FluidCurrentsSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_HIGHER_ENTITIES_PER_THREAD)
    {
        noiseDisturbancesX = GD.Load<NoiseTexture3D>("res://src/microbe_stage/NoiseDisturbanceX.tres") ??
            throw new Exception("Fluid current noise texture couldn't be loaded");

        noiseDisturbancesY = GD.Load<NoiseTexture3D>("res://src/microbe_stage/NoiseDisturbanceY.tres") ??
            throw new Exception("Fluid current noise texture couldn't be loaded");

        noiseCurrentsX = GD.Load<NoiseTexture3D>("res://src/microbe_stage/NoiseCurrentX.tres") ??
            throw new Exception("Fluid current noise texture couldn't be loaded");

        noiseCurrentsY = GD.Load<NoiseTexture3D>("res://src/microbe_stage/NoiseCurrentY.tres") ??
            throw new Exception("Fluid current noise texture couldn't be loaded");
    }

    /// <summary>
    ///   JSON constructor for creating temporary instances used to apply the child properties
    /// </summary>
    [JsonConstructor]
    public FluidCurrentsSystem(float currentsTimePassed) : base(TemporarySystemHelper.GetDummyWorldForLoad(), null)
    {
        this.currentsTimePassed = currentsTimePassed;

        noiseDisturbancesX = null!;
        noiseDisturbancesY = null!;
        noiseCurrentsX = null!;
        noiseCurrentsY = null!;
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    public Vector2 VelocityAt(Vector2 position)
    {
        if (noiseDisturbancesXImage == null || noiseDisturbancesYImage == null
            || noiseCurrentsXImage == null || noiseCurrentsYImage == null)
            return Vector2.Zero;

        // This function's formula should be the same as the one in CurrentsParticles.gdshader
        var scaledPosition = position * POSITION_SCALING * scale;

        float disturbancesX = GetPixel(scaledPosition.X, scaledPosition.Y,
            currentsTimePassed * DISTURBANCE_TIMESCALE * chaoticness, noiseDisturbancesXImage);
        float disturbancesY = GetPixel(scaledPosition.X, scaledPosition.Y,
            currentsTimePassed * DISTURBANCE_TIMESCALE * chaoticness, noiseDisturbancesYImage);

        float currentsX = GetPixel(scaledPosition.X * CURRENTS_STRETCHING_MULTIPLIER,
            scaledPosition.Y, currentsTimePassed * CURRENTS_TIMESCALE * chaoticness, noiseCurrentsXImage);
        float currentsY = GetPixel(scaledPosition.X,
            scaledPosition.Y * CURRENTS_STRETCHING_MULTIPLIER,
            currentsTimePassed * CURRENTS_TIMESCALE * chaoticness, noiseCurrentsYImage);

        var disturbancesVelocity = new Vector2(disturbancesX, disturbancesY);
        var currentsVelocity = new Vector2(Math.Abs(currentsX) > MIN_CURRENT_INTENSITY ? currentsX : 0.0f,
            Math.Abs(currentsY) > MIN_CURRENT_INTENSITY ? currentsY : 0.0f);

        return currentsVelocity.Lerp(disturbancesVelocity, DISTURBANCE_TO_CURRENTS_RATIO) * speed;
    }

    protected override void PreUpdate(float delta)
    {
        base.PreUpdate(delta);

        if (noiseDisturbancesXImage == null || noiseDisturbancesYImage == null
            || noiseCurrentsXImage == null || noiseCurrentsYImage == null)
        {
            var disturbancesX = noiseDisturbancesX.GetData();
            var disturbancesY = noiseDisturbancesY.GetData();
            var currentsX = noiseCurrentsX.GetData();
            var currentsY = noiseCurrentsY.GetData();

            noiseWidth = disturbancesX[0].GetWidth();
            noiseHeight = disturbancesY[0].GetHeight();

            int noiseDepth = noiseDisturbancesX.Depth;
            noiseDisturbancesXImage = new Image[noiseDepth];
            noiseDisturbancesYImage = new Image[noiseDepth];
            noiseCurrentsXImage = new Image[noiseDepth];
            noiseCurrentsYImage = new Image[noiseDepth];

            for (int i = 0; i < noiseDepth; ++i)
            {
                noiseDisturbancesXImage[i] = disturbancesX[i];
                noiseDisturbancesYImage[i] = disturbancesY[i];
                noiseCurrentsXImage[i] = currentsX[i];
                noiseCurrentsYImage[i] = currentsY[i];
            }
        }

        currentsTimePassed += delta;

        if (gameWorld == null)
            throw new InvalidOperationException("GameWorld not set");

        if (gameWorld.Map.CurrentPatch == null)
        {
            GD.PrintErr("Current patch should be set for the fluid currents system to work");
            return;
        }

        var biome = gameWorld.Map.CurrentPatch.BiomeTemplate;

        speed = biome.WaterCurrentSpeed;
        chaoticness = biome.WaterCurrentChaoticness;
        scale = biome.WaterCurrentScale;
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var physics = ref entity.Get<Physics>();

        if (physics.Body == null)
            return;

        ref var position = ref entity.Get<WorldPosition>();
        ref var physicsControl = ref entity.Get<ManualPhysicsControl>();

        var pos = new Vector2(position.Position.X, position.Position.Z);
        var vel = VelocityAt(pos) * Constants.MAX_FORCE_APPLIED_BY_CURRENTS;

        physicsControl.ImpulseToGive += new Vector3(vel.X, 0, vel.Y) * delta;
        physicsControl.PhysicsApplied = false;
    }

    private float GetPixel(float x, float y, float z, Image[] array)
    {
        if (x < 0.0f)
            x = noiseWidth + x % noiseWidth;
        if (y < 0.0f)
            y = noiseHeight + y % noiseHeight;

        return array[(int)z % array.Length].GetPixel((int)x % noiseWidth, (int)y % noiseHeight).R * 2.0f - 1.0f;
    }
}
