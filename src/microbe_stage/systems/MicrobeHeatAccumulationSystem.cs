namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;

/// <summary>
///   Handles heating and cooling of microbes based on localised heat zones
/// </summary>
[With(typeof(CompoundStorage))]
[With(typeof(WorldPosition))]
[With(typeof(OrganelleContainer))]
[With(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(OrganelleContainer))]
[RunsBefore(typeof(ProcessSystem))]
[RunsAfter(typeof(MicrobePhysicsCreationAndSizeSystem))]

// Test if reading noise resource is thread safe in Godot
// [RunsOnMainThread]
public class MicrobeHeatAccumulationSystem : AEntitySetSystem<float>
{
    private readonly Noise noise;

    private GameWorld? gameWorld;

    private float patchTemperatureMiddle;

    public MicrobeHeatAccumulationSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_EXTREME_ENTITIES_PER_THREAD)
    {
        noise = GD.Load<NoiseTexture2D>("res://src/microbe_stage/HeatGradientNoise.tres").Noise ??
            throw new Exception("Heat noise texture doesn't have noise set");
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    public float SampleTemperatureAt(Vector3 position)
    {
        // Scale world position into heat plane UV coordinate space
        position *= Constants.MICROBE_HEAT_NOISE_TO_WORLD_RATIO;

        // And then use modulo to get to noise-space
        var sampleX = position.X % Constants.MICROBE_HEAT_AREA_REPEAT_EVERY_WORLD_COORDINATE * 0.5f;
        var sampleY = position.X % Constants.MICROBE_HEAT_AREA_REPEAT_EVERY_WORLD_COORDINATE * 0.5f;

        // Handle negative sampling positions to be positive
        if (sampleX < 0)
            sampleX = 1 + sampleX;

        if (sampleY < 0)
            sampleY = 1 + sampleY;

        var rawNoise = noise.GetNoise2D(sampleX, sampleY);

        return patchTemperatureMiddle + (rawNoise - 0.5f) * Constants.NOISE_EFFECT_ON_LOCAL_TEMPERATURE;
    }

    protected override void PreUpdate(float delta)
    {
        if (gameWorld == null)
            throw new InvalidOperationException("GameWorld not set");

        if (gameWorld.Map.CurrentPatch == null)
        {
            GD.PrintErr("Current patch should be set for the microbe heat system to work");
            return;
        }

        if (!gameWorld.Map.CurrentPatch.Biome.TryGetCompound(Compound.Temperature, CompoundAmountType.Current,
                out var patchTemperature))
        {
            GD.PrintErr("All patches should have their temperature set (error from microbe heat system)");
            return;
        }

        patchTemperatureMiddle = patchTemperature.Ambient;
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var organelleContainer = ref entity.Get<OrganelleContainer>();

        // Only process microbes that can capture heat
        if (organelleContainer.HeatCollection < 1)
            return;

        ref var properties = ref entity.Get<CellProperties>();
        ref var position = ref entity.Get<WorldPosition>();

        // Adjust heat speed by surface area to the volume-ratio
        var ratio = properties.CalculateSurfaceAreaToVolume(organelleContainer.HexCount);

        var heat = SampleTemperatureAt(position.Position);

        // Initialise temperature to the environment when initially spawning
        if (!properties.HeatInitialized)
        {
            properties.HeatInitialized = true;
            properties.Temperature = heat;
        }
        else
        {
            var change = heat - properties.Temperature;

            change *= ratio * delta * Constants.THERMOPLAST_HEAT_UP_SPEED;

            // When heating up within the right range, give "temperature" compound
            if (change > 0 && properties.Temperature >= Constants.THERMOPLAST_MIN_ATP_TEMPERATURE &&
                properties.Temperature <= Constants.THERMOPLAST_MAX_ATP_TEMPERATURE)
            {
                var compounds = entity.Get<CompoundStorage>().Compounds;

                // Scale given heat by the number of gathering components to make this energy source scale for bigger
                // cells
                compounds.AddCompound(Compound.Temperature,
                    organelleContainer.HeatCollection * change *
                    Constants.TEMPERATURE_CHANGE_TO_TEMPERATURE_MULTIPLIER);
            }

            properties.Temperature += change;
        }
    }
}
