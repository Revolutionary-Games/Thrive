using System.Collections.Generic;

public static class MicrobeInternalCalculations
{
    public static float CalculateSpeed(IEnumerable<OrganelleTemplate> organelles, MembraneType membraneType,
        float membraneRigidity)
    {
        float microbeMass = Constants.MICROBE_BASE_MASS;

        float organelleMovementForce = 0;

        foreach (var organelle in organelles)
        {
            microbeMass += organelle.Definition.Mass;

            if (organelle.Definition.HasComponentFactory<MovementComponentFactory>())
            {
                // Only count flagella that face backwards
                if (organelle.Orientation == 3)
                {
                    organelleMovementForce += Constants.FLAGELLA_BASE_FORCE
                        * organelle.Definition.Components.Movement.Momentum / 100.0f;
                }
            }
        }

        float baseMovementForce = Constants.CELL_BASE_THRUST *
            (membraneType.MovementFactor - membraneRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER);

        float finalSpeed = (baseMovementForce + organelleMovementForce) / microbeMass;

        return finalSpeed;
    }
}
