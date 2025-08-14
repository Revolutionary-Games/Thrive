namespace Systems;

using System;
using System.Runtime.CompilerServices;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
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
    private const float CURRENTS_TIMESCALE = 0.25f;
    private const float POSITION_SCALING = 0.9f;

#pragma warning disable CA2213
    private Texture2D currentsNoise1Texture = null!;
    private Texture2D currentsNoise2Texture = null!;

    private Image currentsNoise1 = null!;
    private Image currentsNoise2 = null!;
#pragma warning restore CA2213

    private bool imagesInitialized;

    private GameWorld? gameWorld;

    private float speed;
    private float chaoticness;
    private float inverseScale;

    [JsonProperty]
    private float currentsTimePassed;

    private int noiseWidth = -1;
    private int noiseHeight = -1;

    public FluidCurrentsSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_HIGHER_ENTITIES_PER_THREAD)
    {
        currentsNoise1Texture = GD.Load<CompressedTexture2D>("res://assets/textures/CurrentsNoise1.png") ??
            throw new Exception("Fluid current noise texture couldn't be loaded");

        currentsNoise2Texture = GD.Load<CompressedTexture2D>("res://assets/textures/CurrentsNoise2.png") ??
            throw new Exception("Fluid current noise texture couldn't be loaded");
    }

    /// <summary>
    ///   JSON constructor for creating temporary instances used to apply the child properties
    /// </summary>
    [JsonConstructor]
    public FluidCurrentsSystem(float currentsTimePassed) : base(TemporarySystemHelper.GetDummyWorldForLoad(), null)
    {
        this.currentsTimePassed = currentsTimePassed;
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    public Vector2 VelocityAt(Vector2 position)
    {
        if (!imagesInitialized)
            return Vector2.Zero;

        // This function's formula should be the same as the one in CurrentsParticles.gdshader
        var scaledPosition = position * POSITION_SCALING * inverseScale;
        var scaledTime = currentsTimePassed * CURRENTS_TIMESCALE * chaoticness;

        Vector2 currents1 = GetPixel(scaledPosition.X + scaledTime, scaledPosition.Y + scaledTime, currentsNoise1);
        Vector2 currents2 = GetPixel(scaledPosition.X - scaledTime, scaledPosition.Y - scaledTime, currentsNoise2);

        var currentsVelocity = currents1 * 2.0f - Vector2.One;

        currentsVelocity *= currents2;

        return currentsVelocity * speed;
    }

    protected override void PreUpdate(float delta)
    {
        base.PreUpdate(delta);

        if (!imagesInitialized)
        {
            currentsNoise1 = currentsNoise1Texture.GetImage();
            currentsNoise2 = currentsNoise2Texture.GetImage();

            if (currentsNoise1 != null && currentsNoise2 != null)
            {
                noiseWidth = currentsNoise1.GetWidth();
                noiseHeight = currentsNoise1.GetHeight();
                imagesInitialized = true;
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

        speed = biome.WaterCurrents.Speed;
        chaoticness = biome.WaterCurrents.Chaoticness;
        inverseScale = biome.WaterCurrents.InverseScale;
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

        float effectStrength = entity.Get<CurrentAffected>().EffectStrength;

        if (effectStrength == 0)
        {
            effectStrength = 1;
        }
        else if (effectStrength < 0)
        {
            return;
        }

        physicsControl.ImpulseToGive += new Vector3(vel.X, 0, vel.Y) * delta * effectStrength;
        physicsControl.PhysicsApplied = false;
    }

    /// <summary>
    ///   Return image pixel's red and green values
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector2 GetPixel(float x, float y, Image image)
    {
        var color = image.GetPixel(((int)x).PositiveModulo(noiseWidth),
            ((int)y).PositiveModulo(noiseHeight));

        return new Vector2(color.R, color.G);
    }
}
