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
/// <remarks>
///   <para>
///     This has <see cref="RunsOnMainThreadAttribute"/> because <see cref="PreUpdate"/> reads a texture into an image.
///     TODO: check if even reading an image is not safe from another thread
///   </para>
/// </remarks>
[With(typeof(CompoundStorage))]
[With(typeof(WorldPosition))]
[With(typeof(OrganelleContainer))]
[With(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(OrganelleContainer))]
[RunsBefore(typeof(ProcessSystem))]
[RunsAfter(typeof(MicrobePhysicsCreationAndSizeSystem))]
[RunsOnMainThread]
public class MicrobeHeatAccumulationSystem : AEntitySetSystem<float>
{
    private readonly NoiseTexture2D noiseSource;

    // Don't dispose: https://github.com/Revolutionary-Games/Thrive/issues/5886
#pragma warning disable CA2213
    private Image? noiseImage;
#pragma warning restore CA2213

    private int noiseWidth;
    private int noiseHeight;

    private GameWorld? gameWorld;

    private float patchTemperatureMiddle;
    private float temperatureVarianceScale = 1;

    public MicrobeHeatAccumulationSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_EXTREME_ENTITIES_PER_THREAD)
    {
        // For easily consistent code with the rendering, we read the noise texture as an image and sample it
        noiseSource = GD.Load<NoiseTexture2D>("res://src/microbe_stage/HeatGradientNoise.tres") ??
            throw new Exception("Heat noise texture couldn't be loaded");
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    public float SampleTemperatureAt(Vector3 position)
    {
        if (noiseImage == null)
            return float.NaN;

        // Convert world position to UV coordinates in the noise texture.
        // The noise plane is centered on its origin, so that's why we add 0.5 here.
        // And to map back to the first "tile" of the texture, we do modulo with one.
        var sampleX = (position.X * Constants.MICROBE_HEAT_NOISE_TO_WORLD_RATIO + 0.5f) % 1.0f;
        var sampleY = (position.Z * Constants.MICROBE_HEAT_NOISE_TO_WORLD_RATIO + 0.5f) % 1.0f;

        // Map negative values back to valid range
        if (sampleX < 0)
            sampleX += 1;

        if (sampleY < 0)
            sampleY += 1;

        // Finally, convert UV coordinates to pixel coordinates (rounding down should be accurate enough)
        var rawNoise = noiseImage.GetPixel((int)(sampleX * noiseWidth), (int)(sampleY * noiseHeight)).R;

        var differenceFromMiddle = rawNoise - Constants.MICROBE_HEAT_NOISE_MIDDLE_POINT;

        return patchTemperatureMiddle + differenceFromMiddle * temperatureVarianceScale;
    }

    protected override void PreUpdate(float delta)
    {
        if (gameWorld == null)
            throw new InvalidOperationException("GameWorld not set");

        // Grab the noise image once it is ready
        if (noiseImage == null)
        {
            noiseImage = noiseSource.GetImage();

            if (noiseImage != null)
            {
                noiseWidth = noiseImage.GetWidth();
                noiseHeight = noiseImage.GetHeight();
            }
        }

        if (gameWorld.Map.CurrentPatch == null)
        {
            GD.PrintErr("Current patch should be set for the microbe heat system to work");
            return;
        }

        temperatureVarianceScale = gameWorld.Map.CurrentPatch.BiomeTemplate.TemperatureVarianceScale;

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
        // Can't process when the noise isn't ready yet
        if (noiseImage == null)
            return;

        ref var organelleContainer = ref entity.Get<OrganelleContainer>();

        // Only process microbes that can capture heat
        if (organelleContainer.HeatCollection < 1)
            return;

        ref var properties = ref entity.Get<CellProperties>();
        ref var position = ref entity.Get<WorldPosition>();

        // Adjust heat speed by surface area to the volume-ratio
        var ratio = properties.CalculateSurfaceAreaToVolume(organelleContainer.HexCount);

        var heat = SampleTemperatureAt(position.Position);

        if (float.IsNaN(heat))
        {
            GD.PrintErr("Generated NaN temperature for microbe");
            return;
        }

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
