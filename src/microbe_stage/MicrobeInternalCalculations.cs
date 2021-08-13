using System.Collections.Generic;
using Godot;

public static class MicrobeInternalCalculations
{
    public static float CalculateSpeed(IEnumerable<OrganelleTemplate> organelles, MembraneType membraneType,
        float membraneRigidity)
    {
        float microbeMass = Constants.MICROBE_BASE_MASS;

        float organelleMovementForce = 0;

        Vector3 forwardsDirection = new Vector3(0, 0, -1);

        foreach (var organelle in organelles)
        {
            microbeMass += organelle.Definition.Mass;

            if (organelle.Definition.HasComponentFactory<MovementComponentFactory>())
            {
                Vector3 organelleDirection = (Hex.AxialToCartesian(new Hex(0, 0))
                    - Hex.AxialToCartesian(organelle.Position)).Normalized();

                float directionFactor = organelleDirection.Dot(forwardsDirection);

                organelleMovementForce += Constants.FLAGELLA_BASE_FORCE
                    * organelle.Definition.Components.Movement.Momentum / 100.0f
                    * directionFactor;
            }
        }

        float baseMovementForce = Constants.CELL_BASE_THRUST *
            (membraneType.MovementFactor - membraneRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER);

        float finalSpeed = (baseMovementForce + organelleMovementForce) / microbeMass;

        return finalSpeed;
    }

    public static float EnergyGenerationScore(MicrobeSpecies species, Compound compound)
    {
        Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
        Compound atp = SimulationParameters.Instance.GetCompound("atp");

        var energyCreationScore = 0.0f;
        foreach (var organelle in species.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.ContainsKey(compound))
                {
                    if (process.Process.Outputs.ContainsKey(glucose))
                    {
                        energyCreationScore += process.Process.Outputs[glucose]
                            / process.Process.Inputs[compound] * Constants.AUTO_EVO_GLUCOSE_USE_SCORE_MULTIPLIER;
                    }

                    if (process.Process.Outputs.ContainsKey(atp))
                    {
                        energyCreationScore += process.Process.Outputs[atp]
                            / process.Process.Inputs[compound] * Constants.AUTO_EVO_ATP_USE_SCORE_MULTIPLIER;
                    }
                }
            }
        }

        return energyCreationScore;
    }
}
