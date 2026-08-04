namespace ThriveTest.MicrobeStage;

using System;
using Arch.Core;
using Godot;
using Xunit;

public class SlimeJetFramerateTests
{
    [Fact]
    public void SlimeJet_ForceIsNotFramerateDependent()
    {
        using var world = World.Create();
        var entity = world.Create();

        var jet = new SlimeJetComponent
        {
            Active = true,
        };

        // 10 units of slime per second
        float emissionRate = 10.0f;

        // Scenario 1: 60 FPS (delta = 1/60)
        float delta60 = 1.0f / 60.0f;
        float slimePerFrame60 = emissionRate * delta60;
        double slimeUsed60 = 0;

        Vector3 totalForce60 = Vector3.Zero;
        for (int i = 0; i < 60; ++i)
        {
            jet.AddQueuedForce(entity, slimePerFrame60, delta60);
            jet.ConsumeMovementForce(out var force);

            // Simulate the physics engine multiplying by delta once more
            totalForce60 += force * delta60;

            slimeUsed60 += slimePerFrame60;
        }

        // Scenario 2: 10 FPS (delta = 1/10)
        float delta10 = 1.0f / 10.0f;
        float slimePerFrame10 = emissionRate * delta10;
        double slimeUsed10 = 0;

        Vector3 totalForce10 = Vector3.Zero;
        for (int i = 0; i < 10; ++i)
        {
            jet.AddQueuedForce(entity, slimePerFrame10, delta10);
            jet.ConsumeMovementForce(out var force);

            // Simulate the physics engine multiplying by delta once more
            totalForce10 += force * delta10;

            slimeUsed10 += slimePerFrame10;
        }

        // They should be equal now (assuming no bug)
        Assert.Equal(totalForce60.Length(), totalForce10.Length(), 1.0f);

        // And slime consumption should be the same
        const int requiredMatchingDecimals = 5;
        Assert.Equal(Math.Round(slimeUsed60, requiredMatchingDecimals),
            Math.Round(slimeUsed10, requiredMatchingDecimals));
    }
}
