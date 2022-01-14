using System.Collections.Generic;
using System.Linq;
using Godot;

public static class MicrobeInternalCalculations
{
    public static Vector3 MaximumSpeedDirection(IEnumerable<OrganelleTemplate> organelles)
    {
        Vector3 maximumMovementDirection = Vector3.Zero;

        var movementOrganelles = organelles.Where(o => o.Definition.HasComponentFactory<MovementComponentFactory>())
            .ToList();

        foreach (var organelle in movementOrganelles)
        {
            maximumMovementDirection += GetOrganelleDirection(organelle);
        }

        // After calculating the sum of all organelle directions we subtract the movement components which
        // are symmetric and we choose the one who would benefit the max-speed the most.
        foreach (var organelle in movementOrganelles)
        {
            maximumMovementDirection = ChooseFromSymmetricFlagella(movementOrganelles,
                organelle, maximumMovementDirection);
        }

        // If the flagella are positioned symmetrically we assume the forward position as default
        if (maximumMovementDirection == Vector3.Zero)
            return Vector3.Forward;

        return maximumMovementDirection;
    }

    public static Vector3 GetOrganelleDirection(OrganelleTemplate organelle)
    {
        return (Hex.AxialToCartesian(new Hex(0, 0)) - Hex.AxialToCartesian(organelle.Position)).Normalized();
    }

    public static float CalculateSpeed(IEnumerable<OrganelleTemplate> organelles, MembraneType membraneType,
        float membraneRigidity)
    {
        float microbeMass = Constants.MICROBE_BASE_MASS;

        float organelleMovementForce = 0;

        // For each direction we calculate the organelles contribution to the movement force
        float forwardsDirectionMovementForce = 0;
        float backwardsDirectionMovementForce = 0;
        float leftwardDirectionMovementForce = 0;
        float rightwardDirectionMovementForce = 0;

        // Force factor for each direction
        float forwardDirectionFactor;
        float backwardDirectionFactor;
        float rightDirectionFactor;
        float leftDirectionFactor;

        var organellesList = organelles.ToList();

        foreach (var organelle in organellesList)
        {
            microbeMass += organelle.Definition.Mass;

            if (organelle.Definition.HasComponentFactory<MovementComponentFactory>())
            {
                Vector3 organelleDirection = GetOrganelleDirection(organelle);

                // We decompose the vector of the organelle orientation in 2 vectors, forward and right
                // To get the backward and left is easy because they are the opposite of those former 2
                forwardDirectionFactor = organelleDirection.Dot(Vector3.Forward);
                backwardDirectionFactor = -forwardDirectionFactor;
                rightDirectionFactor = organelleDirection.Dot(Vector3.Right);
                leftDirectionFactor = -rightDirectionFactor;

                float movementConstant = Constants.FLAGELLA_BASE_FORCE
                    * organelle.Definition.Components.Movement.Momentum / 100.0f;

                // We get the movement force for every direction as well
                forwardsDirectionMovementForce += MovementForce(movementConstant, forwardDirectionFactor);
                backwardsDirectionMovementForce += MovementForce(movementConstant, backwardDirectionFactor);
                rightwardDirectionMovementForce += MovementForce(movementConstant, rightDirectionFactor);
                leftwardDirectionMovementForce += MovementForce(movementConstant, leftDirectionFactor);
            }
        }

        var maximumMovementDirection = MaximumSpeedDirection(organellesList);

        // Maximum-force direction is not normalized so we need to normalize it here
        maximumMovementDirection = maximumMovementDirection.Normalized();

        // Calculate the maximum total force-factors in the maximum-force direction
        forwardDirectionFactor = maximumMovementDirection.Dot(Vector3.Forward);
        backwardDirectionFactor = -forwardDirectionFactor;
        rightDirectionFactor = maximumMovementDirection.Dot(Vector3.Right);
        leftDirectionFactor = -rightDirectionFactor;

        // Add each movement force to the total movement force in the maximum-force direction.
        organelleMovementForce += MovementForce(forwardsDirectionMovementForce, forwardDirectionFactor);
        organelleMovementForce += MovementForce(backwardsDirectionMovementForce, backwardDirectionFactor);
        organelleMovementForce += MovementForce(rightwardDirectionMovementForce, rightDirectionFactor);
        organelleMovementForce += MovementForce(leftwardDirectionMovementForce, leftDirectionFactor);

        float baseMovementForce = Constants.CELL_BASE_THRUST *
            (membraneType.MovementFactor - membraneRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER);

        float finalSpeed = (baseMovementForce + organelleMovementForce) / microbeMass;

        return finalSpeed;
    }

    private static float MovementForce(float movementForce, float directionFactor)
    {
        if (directionFactor < 0)
            return 0;

        return movementForce * directionFactor;
    }

    /// <summary>
    ///  Symmetric flagella are a corner case for speed calculations because the sum of all
    ///  directions is kind of broken in their case, so we have to choose which one of the symmetric flagella
    ///  we must discard from the direction calculation
    ///  Here we only discard if the flagella we input is the "bad" one
    /// </summary>
    private static Vector3 ChooseFromSymmetricFlagella(IEnumerable<OrganelleTemplate> organelles,
        OrganelleTemplate testedOrganelle, Vector3 maximumMovementDirection)
    {
        foreach (var organelle in organelles)
        {
            if (organelle != testedOrganelle &&
                organelle.Position + testedOrganelle.Position == new Hex(0, 0))
            {
                var organelleLength = (maximumMovementDirection - GetOrganelleDirection(organelle)).Length();
                var testedOrganelleLength = (maximumMovementDirection -
                    GetOrganelleDirection(testedOrganelle)).Length();

                if (organelleLength > testedOrganelleLength)
                    return maximumMovementDirection;

                return maximumMovementDirection - GetOrganelleDirection(testedOrganelle);
            }
        }

        return maximumMovementDirection;
    }
}
