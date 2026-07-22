namespace AutoEvo;

using System;
using System.Collections.Generic;
using static CommonMutationFunctions;

public class ChangeMembraneType : IMutationStrategy<Species>
{
    private MembraneType membraneType;

    public ChangeMembraneType(string membraneType)
    {
        this.membraneType = SimulationParameters.Instance.GetMembrane(membraneType);
    }

    public bool Repeatable => false;

    public List<Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (baseSpecies is not MicrobeSpecies baseMicrobeSpecies)
            return null;

        if (baseMicrobeSpecies.MembraneType == membraneType)
            return null;

        if (mp < membraneType.EditorCost)
            return null;

        var organelles = baseMicrobeSpecies.Organelles;

        for (int i = 0; i < organelles.Count; ++i)
        {
            if (organelles[i].Definition.IsIncompatibleWithMembrane(membraneType))
                return null;
        }

        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        newSpecies.MembraneType = membraneType;

        return [new Mutant(newSpecies, mp - membraneType.EditorCost)];
    }
}
