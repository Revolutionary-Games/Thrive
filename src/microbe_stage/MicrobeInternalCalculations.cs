using System.Collections.Generic;
using Godot;
using System.Linq;
public static class MicrobeInternalCalculations
{
    private static float MovementForce(float movementForce, float directionFactor)
    {
        if (directionFactor < 0)
            return 0;
        return movementForce * directionFactor;
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
        float forwardDirectionFactor ;
        float backwardDirectionFactor ;
        float rightwardDirectionFactor ;
        float leftwardDirectionFactor ;

        Vector3 maximumMovementDirection = Vector3.Zero;
        foreach (var organelle in organelles)
        {
            microbeMass += organelle.Definition.Mass;

            if (organelle.Definition.HasComponentFactory<MovementComponentFactory>())
            {
                Vector3 organelleDirection = (Hex.AxialToCartesian(new Hex(0, 0))
                    - Hex.AxialToCartesian(organelle.Position)).Normalized();

                if (maximumMovementDirection == Vector3.Zero)
                    maximumMovementDirection = organelleDirection;
                else
                    maximumMovementDirection += organelleDirection;

                // We decompose the vector of the organelle orientation in 2 vectors, forward and rightward
                forwardDirectionFactor = organelleDirection.Dot(Vector3.Forward);
                backwardDirectionFactor = -forwardDirectionFactor;
                rightwardDirectionFactor = organelleDirection.Dot(Vector3.Right);
                leftwardDirectionFactor = -rightwardDirectionFactor;

                float movementConstant = Constants.FLAGELLA_BASE_FORCE
                    * organelle.Definition.Components.Movement.Momentum / 100.0f;

                // We get the movement force for every direction as well
                forwardsDirectionMovementForce += MovementForce(movementConstant, forwardDirectionFactor);
                backwardsDirectionMovementForce += MovementForce(movementConstant, backwardDirectionFactor);
                rightwardDirectionMovementForce += MovementForce(movementConstant, rightwardDirectionFactor);
                leftwardDirectionMovementForce += MovementForce(movementConstant, leftwardDirectionFactor);
            }
        }

        // After getting the maximum-force direction we normalize it
        maximumMovementDirection = maximumMovementDirection.Normalized();

        // Calculate the maximum total force-factors in the maximum-force direction
        forwardDirectionFactor = maximumMovementDirection.Dot(Vector3.Forward);
        backwardDirectionFactor = -forwardDirectionFactor;
        rightwardDirectionFactor = maximumMovementDirection.Dot(Vector3.Right);
        leftwardDirectionFactor = -rightwardDirectionFactor;

        // Create a list so we can get the maximum value easier
        var list = new List<float> {forwardsDirectionMovementForce, backwardsDirectionMovementForce,
        leftwardDirectionMovementForce, rightwardDirectionMovementForce};

        // The microbe's max speed is calculated using
        // the greatest movement force from all direction's forces
        organelleMovementForce = list.Max();

        float baseMovementForce = Constants.CELL_BASE_THRUST *
            (membraneType.MovementFactor - membraneRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER);

        float finalSpeed = (baseMovementForce + organelleMovementForce) / microbeMass;

        return finalSpeed;
    }
}
