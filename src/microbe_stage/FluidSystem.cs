using System;
using Godot;

public class FluidSystem
{
    /*
        private const float MaxForceApplied = 0.525f;
    */
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

    /*
        private readonly Vector2 scale = new Vector2(0.05f, 0.05f);
    */

    private readonly Node worldRoot;

    private float millisecondsPassed;

    public FluidSystem(Node worldRoot)
    {
        noiseDisturbancesX = new PerlinNoise(69);
        noiseDisturbancesY = new PerlinNoise(13);
        noiseCurrentsX = new PerlinNoise(420);
        noiseCurrentsY = new PerlinNoise(1337);
        this.worldRoot = worldRoot;
    }

    public void Process(float delta)
    {
        millisecondsPassed += delta / 1000.0f;
    }

    public void PhysicsProcess(float delta)
    {
        _ = delta;

        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.FLUID_EFFECT_GROUP);

        foreach (Node entity in nodes)
        {
            var body = entity as RigidBody;

            if (body == null)
            {
                GD.PrintErr("A node has been put in the fluid effect group " +
                    "but it isn't a rigidbody");
                continue;
            }

            var pos = new Vector2(body.Translation.x, body.Translation.z);
            var vel = VelocityAt(pos) * Constants.MAX_FORCE_APPLIED_BY_CURRENTS;
            body.ApplyCentralImpulse(new Vector3(vel.x, 0, vel.y));
        }
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
