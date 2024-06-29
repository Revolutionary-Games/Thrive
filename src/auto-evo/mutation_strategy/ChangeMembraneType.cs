namespace AutoEvo;

using System;
using System.Collections.Generic;

public class ChangeMembraneType : IMutationStrategy<MicrobeSpecies>
{
    private MembraneType membraneType;

    public ChangeMembraneType(MembraneType membraneType)
    {
        this.membraneType = membraneType;
    }

    public bool Repeatable => false;

    public List<Tuple<MicrobeSpecies, float>> MutationsOf(MicrobeSpecies baseSpecies, float mp)
    {
        if (baseSpecies.MembraneType == membraneType)
            return [];

        if (mp < membraneType.EditorCost)
            return [];

        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        newSpecies.MembraneType = membraneType;

        return [Tuple.Create(newSpecies, mp - membraneType.EditorCost)];
    }
}
