using System;
using Godot;

public class FluidSystem
{
    /*
        private const float MaxForceApplied = 0.525f;
    */
    private const float DISTURBANCE_TIMESCALE = 0.001f;
    private const float CURRENTS_TIMESCALE = 0.001f / 500.0f;
    private const float CURRENTS_STRETCHING_MULTIPLIER = 1.0f / 10.0f;
    private const float MIN_CURRENT_INTENSITY = 0.4f;
    private const float DISTURBANCE_TO_CURRENTS_RATIO = 0.15f;
    private const float POSITION_SCALING = 0.9f;

    private readonly FastNoiseLite noiseDisturbancesX;
    private readonly FastNoiseLite noiseDisturbancesY;
    private readonly FastNoiseLite noiseCurrentsX;
    private readonly FastNoiseLite noiseCurrentsY;

    /*
        private readonly Vector2 scale = new Vector2(0.05f, 0.05f);
    */

    private readonly Node worldRoot;

    // TODO: this should be probably saved in the future to make currents consistent after loading a save
    private float millisecondsPassed;

    public FluidSystem(Node worldRoot)
    {
        noiseDisturbancesX = new FastNoiseLite(69);
        noiseDisturbancesX.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

        noiseDisturbancesY = new FastNoiseLite(13);
        noiseDisturbancesY.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

        noiseCurrentsX = new FastNoiseLite(420);
        noiseCurrentsX.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

        noiseCurrentsY = new FastNoiseLite(1337);
        noiseCurrentsY.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        this.worldRoot = worldRoot;
    }

    public void Process(float delta)
    {
        millisecondsPassed += delta / 1000.0f;
    }

    public void PhysicsProcess(float delta)
    {
        _ = delta;
        foreach (var body in worldRoot.GetChildrenToProcess<RigidBody>(Constants.FLUID_EFFECT_GROUP))
        {
            var pos = new Vector2(body.Translation.x, body.Translation.z);
            var vel = VelocityAt(pos) * Constants.MAX_FORCE_APPLIED_BY_CURRENTS;
            body.ApplyCentralImpulse(new Vector3(vel.x, 0, vel.y));
        }
    }

    public Vector2 VelocityAt(Vector2 position)
    {
        var scaledPosition = position * POSITION_SCALING;

        float disturbancesX = noiseDisturbancesX.GetNoise(scaledPosition.x, scaledPosition.y,
            millisecondsPassed * DISTURBANCE_TIMESCALE);
        float disturbancesY = noiseDisturbancesY.GetNoise(scaledPosition.x, scaledPosition.y,
            millisecondsPassed * DISTURBANCE_TIMESCALE);

        float currentsX = noiseCurrentsX.GetNoise(scaledPosition.x * CURRENTS_STRETCHING_MULTIPLIER,
            scaledPosition.y, millisecondsPassed * CURRENTS_TIMESCALE);
        float currentsY = noiseCurrentsY.GetNoise(scaledPosition.x, scaledPosition.y * CURRENTS_STRETCHING_MULTIPLIER,
            millisecondsPassed * CURRENTS_TIMESCALE);

        var disturbancesVelocity = new Vector2(disturbancesX, disturbancesY);
        var currentsVelocity = new Vector2(
            Math.Abs(currentsX) > MIN_CURRENT_INTENSITY ? currentsX : 0.0f,
            Math.Abs(currentsY) > MIN_CURRENT_INTENSITY ? currentsY : 0.0f);

        return (disturbancesVelocity * DISTURBANCE_TO_CURRENTS_RATIO) +
            (currentsVelocity * (1.0f - DISTURBANCE_TO_CURRENTS_RATIO));
    }
}
