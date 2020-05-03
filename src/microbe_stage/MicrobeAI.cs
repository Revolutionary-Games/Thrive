using System;
using Godot;

/// <summary>
///   AI for a single Microbe
/// </summary>
public class MicrobeAI
{
    private readonly Microbe microbe;

    public MicrobeAI(Microbe microbe)
    {
        this.microbe = microbe ?? throw new ArgumentException(nameof(microbe));
    }

    public void Think(float delta, Random random)
    {
        SetRandomTargetAndSpeed(random);
    }

    /// <summary>
    ///   This makes the microbe to do some random movement, used by the AI when nothing else should be done
    /// </summary>
    private void SetRandomTargetAndSpeed(Random random)
    {
        // Set a random nearby look at location
        microbe.LookAtPoint = microbe.Translation + new Vector3(
            random.Next(-200, 201), 0, random.Next(-200, 201));

        // And random movement speed
        microbe.MovementDirection = new Vector3(0, 0, (float)(-1 * random.NextDouble()));
    }
}
