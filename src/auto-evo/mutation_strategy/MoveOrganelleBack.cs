using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

internal class MoveOrganelleBack : IMutationStrategy<MicrobeSpecies>
{
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;

    public MoveOrganelleBack(Func<OrganelleDefinition, bool> criteria)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
    }

    public bool Repeatable => true;

    public List<Tuple<MicrobeSpecies, double>>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (mp < Constants.ORGANELLE_MOVE_COST)
            return null;

        var mutated = new List<Tuple<MicrobeSpecies, double>>();

        // TODO: try to avoid these temporary list allocations
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var workMemory3 = new HashSet<Hex>();

        int organelleCount = baseSpecies.Organelles.Count;
        for (int i = 0; i < organelleCount; ++i)
        {
            var organelle = baseSpecies.Organelles[i];

            if (!allOrganelles.Contains(organelle.Definition))
                continue;

            MicrobeSpecies newSpecies = (MicrobeSpecies)baseSpecies.Clone();
            newSpecies.Organelles.Remove(organelle);

            if (CommonMutationFunctions.AddOrganelle(organelle.Definition, CommonMutationFunctions.Direction.Rear,
                    newSpecies, workMemory1, workMemory2, workMemory3, random))
            {
                mutated.Add(Tuple.Create(newSpecies, mp - Constants.ORGANELLE_MOVE_COST));
            }
        }

        return mutated;
    }
}
