using System;
using Godot;

/// <summary>
///   Main script on each cell in the game
/// </summary>
public class Microbe : RigidBody, ISpawned
{
    /// <summary>
    ///   The stored compounds in this microbe
    /// </summary>
    public readonly CompoundBag Compounds = new CompoundBag(0.0f);

    /// <summary>
    ///   The point towards which the microbe will move to point to
    /// </summary>
    public Vector3 LookAtPoint = new Vector3(0, 0, -1);

    /// <summary>
    ///   The direction the microbe wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection = new Vector3(0, 0, 0);

    public int HexCount
    {
        get
        {
            // TODO: add computation and caching for this
            return 1;
        }
    }

    public float DespawnRadiusSqr { get; set; }

    public Node SpawnedNode
    {
        get
        {
            return this;
        }
    }

    public override void _Ready()
    {
        // TODO: reimplement capacity calculation
        Compounds.Capacity = 50.0f;

        // Add some ATP for testing
        Compounds.AddCompound("atp", 30);
    }

    public override void _Process(float delta)
    {
        if (MovementDirection != new Vector3(0, 0, 0))
        {
            // Make sure the direction is normalized
            MovementDirection = MovementDirection.Normalized();

            Vector3 totalMovement = new Vector3(0, 0, 0);

            totalMovement += DoBaseMovementForce(delta);

            ApplyMovementImpulse(totalMovement, delta);
        }

        // ApplyRotation();
    }

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        // TODO: should movement also be applied here?

        state.Transform = GetNewPhysicsRotation(state.Transform);
    }

    private Vector3 DoBaseMovementForce(float delta)
    {
        Constants constants =
            Constants.Instance;

        var cost = (constants.BASE_MOVEMENT_ATP_COST * HexCount) * delta;

        var got = Compounds.TakeCompound("atp", cost);

        float force = constants.CELL_BASE_THRUST;

        // Halve speed if out of ATP
        if (got < cost)
        {
            // Not enough ATP to move at full speed
            force *= 0.5f;
        }

        return Transform.basis.Xform(MovementDirection.Normalized() * force);

        // * microbeComponent.movementFactor *
        // (SimulationParameters::membraneRegistry().getTypeData(
        // microbeComponent.species.membraneType).movementFactor -
        //     microbeComponent.species.membraneRigidity *
        // MEMBRANE_RIGIDITY_MOBILITY_MODIFIER));
    }

    private void ApplyMovementImpulse(Vector3 movement, float delta)
    {
        if (movement.x == 0.0f && movement.z == 0.0f)
            return;

        ApplyCentralImpulse(movement * delta);
    }

    /// <summary>
    ///   Just slerps towards a fixed amount the target point
    /// </summary
    private Transform GetNewPhysicsRotation(Transform transform)
    {
        var target = Transform.LookingAt(LookAtPoint, new Vector3(0, 1, 0));

        return new Transform(Transform.basis.Slerp(target.basis, 0.2f), Transform.origin);
    }
}
