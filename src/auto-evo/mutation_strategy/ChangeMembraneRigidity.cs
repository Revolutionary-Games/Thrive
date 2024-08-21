namespace AutoEvo;

using System;
using System.Collections.Generic;
using Godot;

public class ChangeMembraneRigidity : IMutationStrategy<MicrobeSpecies>
{
    public bool Lower;

    public ChangeMembraneRigidity(bool lower)
    {
        Lower = lower;
    }

    public bool Repeatable => true;

    public List<Tuple<MicrobeSpecies, float>>? MutationsOf(MicrobeSpecies baseSpecies, float mp)
    {
        const float change = Constants.AUTO_EVO_MUTATION_RIGIDITY_STEP;
        const float mpCost = change * 10 * 2;

        if (mp < mpCost)
            return null;

        if (Lower && baseSpecies.MembraneRigidity == -1.0f)
            return null;

        if (!Lower && baseSpecies.MembraneRigidity == 1.0f)
            return null;

        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        if (Lower)
        {
            newSpecies.MembraneRigidity -= change;
        }
        else
        {
            newSpecies.MembraneRigidity += change;
        }

        newSpecies.MembraneRigidity = Mathf.Max(Mathf.Min(newSpecies.MembraneRigidity, 1), -1);

        return [Tuple.Create(newSpecies, mp - mpCost)];
    }
}
