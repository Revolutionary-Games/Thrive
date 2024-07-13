using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

internal class MoveOrganelleBack : IMutationStrategy<MicrobeSpecies>
{
    private readonly OrganelleDefinition[] allOrganelles;

    public MoveOrganelleBack(Func<OrganelleDefinition, bool> criteria)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToArray();
    }

    public bool Repeatable => true;

    public List<Tuple<MicrobeSpecies, float>> MutationsOf(MicrobeSpecies baseSpecies, float mp)
    {
        if (mp < Constants.ORGANELLE_MOVE_COST)
        {
            return [];
        }

        var mutated = new List<Tuple<MicrobeSpecies, float>>();

        foreach (OrganelleTemplate organelle in baseSpecies.Organelles.Where(x => allOrganelles.Contains(x.Definition)))
        {
            MicrobeSpecies newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            newSpecies.Organelles.Remove(organelle);

            CommonMutationFunctions.AddOrganelle(organelle.Definition, CommonMutationFunctions.Direction.Rear, newSpecies, new Random());

            mutated.Add(Tuple.Create(newSpecies, mp - Constants.ORGANELLE_MOVE_COST));
        }

        return mutated;
    }
}
