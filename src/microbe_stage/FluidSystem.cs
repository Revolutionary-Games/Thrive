using System;
using Godot;

public class FluidSystem
{
    private const float MaxForceApplied = 0.525f;
    private const float DisturbanceTimescale = 0.001f;
    private const float CurrentsTimescale = 0.001f / 500.0f;
    private const float CurrentsStretchingMultiplier = 1.0f / 10.0f;
    private const float MinCurrentIntensity = 0.4f;
    private const float DisturbanceToCurrentsRatio = 0.15f;
    private const float PositionScaling = 0.05f;

    private readonly PerlinNoise noiseDisturbancesX;
    private readonly PerlinNoise noiseDisturbancesY;
    private readonly PerlinNoise noiseCurrentsX;
    private readonly PerlinNoise noiseCurrentsY;

    private readonly Vector2 scale = new Vector2(0.05f, 0.05f);

    private float millisecondsPassed = 0.0f;

    public FluidSystem()
    {
        noiseDisturbancesX = new PerlinNoise(69);
        noiseDisturbancesY = new PerlinNoise(13);
        noiseCurrentsX = new PerlinNoise(420);
        noiseCurrentsY = new PerlinNoise(1337);
    }

    public void Process(float delta)
    {
        millisecondsPassed += delta / 1000.0f;
    }

    public Vector2 VelocityAt(Vector2 position)
    {
        var scaledPosition = position * PositionScaling;

        float disturbances_x =
             ((float)noiseDisturbancesX.Noise(scaledPosition.x, scaledPosition.y,
                 millisecondsPassed * DisturbanceTimescale) *
                 2.0f) -
             1.0f;

        float disturbances_y =
             ((float)noiseDisturbancesY.Noise(scaledPosition.x, scaledPosition.y,
                 millisecondsPassed * DisturbanceTimescale) *
                 2.0f) -
             1.0f;

        float currents_x =
             ((float)noiseCurrentsX.Noise(scaledPosition.x * CurrentsStretchingMultiplier,
                 scaledPosition.y, millisecondsPassed * CurrentsTimescale) *
                 2.0f) -
             1.0f;
        float currents_y =
             ((float)noiseCurrentsY.Noise(scaledPosition.x,
                 scaledPosition.y * CurrentsStretchingMultiplier,
                 millisecondsPassed * CurrentsTimescale) *
                 2.0f) -
             1.0f;

        var disturbancesVelocity = new Vector2(disturbances_x, disturbances_y);
        var currentsVelocity = new Vector2(
            Math.Abs(currents_x) > MinCurrentIntensity ? currents_x : 0.0f,
            Math.Abs(currents_y) > MinCurrentIntensity ? currents_y : 0.0f);

        return (disturbancesVelocity * DisturbanceToCurrentsRatio) +
                (currentsVelocity * (1.0f - DisturbanceToCurrentsRatio));
    }
}
