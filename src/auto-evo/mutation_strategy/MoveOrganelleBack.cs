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

    public List<Tuple<MicrobeSpecies, float>> MutationsOf(MicrobeSpecies baseSpecies, float mp)
    {
        if (mp < Constants.ORGANELLE_MOVE_COST)
        {
            return [];
        }

        var mutated = new List<Tuple<MicrobeSpecies, float>>();

        var random = new Random();
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (OrganelleTemplate organelle in baseSpecies.Organelles.Where(x => allOrganelles.Contains(x.Definition)))
        {
            MicrobeSpecies newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            newSpecies.Organelles.Remove(organelle);

            CommonMutationFunctions.AddOrganelle(organelle.Definition, CommonMutationFunctions.Direction.Rear,
                newSpecies, workMemory1, workMemory2, random);

            mutated.Add(Tuple.Create(newSpecies, mp - Constants.ORGANELLE_MOVE_COST));
        }

        return mutated;
    }
}
