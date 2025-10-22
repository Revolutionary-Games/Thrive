namespace Systems;

using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;
using Godot;
using SharedBase.Archive;
using World = Arch.Core.World;

/// <summary>
///   Gives a push from currents in a fluid to physics entities (that have <see cref="ManualPhysicsControl"/>).
///   Only acts on entities marked with <see cref="CurrentAffected"/>.
/// </summary>
[ReadsComponent(typeof(CurrentAffected))]
[ReadsComponent(typeof(Physics))]
[ReadsComponent(typeof(WorldPosition))]
[RuntimeCost(8)]
[RunsOnMainThread]
public partial class FluidCurrentsSystem : BaseSystem<World, float>, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public FluidCurrentDisplay? FluidCurrentDisplay;

    // The following constants should be the same as in CurrentsParticles.gdshader
    private const float CURRENTS_TIMESCALE = 0.25f;
    private const float POSITION_SCALING = 0.9f;

#pragma warning disable CA2213
    private Texture2D currentsNoise1Texture;
    private Texture2D currentsNoise2Texture;

    private Image currentsNoise1 = null!;
    private Image currentsNoise2 = null!;
#pragma warning restore CA2213

    private bool imagesInitialized;

    private GameWorld? gameWorld;

    private float speed;
    private float chaoticness;
    private float inverseScale;

    private float currentsTimePassed;

    private int noiseWidth = -1;
    private int noiseHeight = -1;

    public FluidCurrentsSystem(World world, float currentsTimePassed) : base(world)
    {
        currentsNoise1Texture = GD.Load<CompressedTexture2D>("res://assets/textures/CurrentsNoise1.png") ??
            throw new Exception("Fluid current noise texture couldn't be loaded");

        currentsNoise2Texture = GD.Load<CompressedTexture2D>("res://assets/textures/CurrentsNoise2.png") ??
            throw new Exception("Fluid current noise texture couldn't be loaded");

        this.currentsTimePassed = currentsTimePassed;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.FluidCurrentsSystem;

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

    public override void BeforeUpdate(in float delta)
    {
        if (!imagesInitialized)
        {
            currentsNoise1 = currentsNoise1Texture.GetImage();
            currentsNoise2 = currentsNoise2Texture.GetImage();
            noiseWidth = currentsNoise1.GetWidth();
            noiseHeight = currentsNoise2.GetHeight();
            imagesInitialized = true;
        }

        currentsTimePassed += delta;
        FluidCurrentDisplay?.UpdateTime(currentsTimePassed);

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

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(currentsTimePassed);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        currentsTimePassed = reader.ReadFloat();
    }

    [Query(Parallel = true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref Physics physics, ref WorldPosition position,
        ref ManualPhysicsControl physicsControl, ref CurrentAffected currentAffected, in Entity entity)
    {
        if (physics.Body == null)
            return;

        var pos = new Vector2(position.Position.X, position.Position.Z);
        var vel = VelocityAt(pos) * Constants.MAX_FORCE_APPLIED_BY_CURRENTS;

        float effectStrength = currentAffected.EffectStrength;

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
