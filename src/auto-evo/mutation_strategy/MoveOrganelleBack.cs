using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using static CommonMutationFunctions;

internal class MoveOrganelleBack : IMutationStrategy<Species>
{
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;

    public MoveOrganelleBack(Func<OrganelleDefinition, bool> criteria)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
    }

    public bool Repeatable => true;

    public List<Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (baseSpecies is not MicrobeSpecies baseMicrobeSpecies)
            return null;

        if (mp < Constants.ORGANELLE_MOVE_COST)
            return null;

        var mutated = new List<Mutant>();

        // TODO: try to avoid these temporary list allocations
        var workMemory = new MutationWorkMemory();

        foreach (OrganelleTemplate organelle in baseMicrobeSpecies.Organelles.Where(x =>
                     allOrganelles.Contains(x.Definition)))
        {
            MicrobeSpecies newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            newSpecies.Organelles.Remove(organelle);

            if (AddOrganelle(organelle.Definition, Direction.Rear, newSpecies, workMemory.WorkingMemory1,
                    workMemory.WorkingMemory2, workMemory.WorkingMemory3, random))
            {
                // Moving a bridging organelle can leave disconnected groups behind.
                if (newSpecies.Organelles.GetIslandHexes(workMemory.WorkingMemory1, workMemory.WorkingMemory3,
                        workMemory.WorkingMemory2, workMemory.WorkingMemory4) > 0)
                {
                    continue;
                }

                // Add mutation attempt only if was able to place the organelle
                // TODO: maybe this should add the attempt anyway as this may act as a separate remove organelle step
                // for things that cannot be moved?
                mutated.Add(new Mutant(newSpecies, mp - Constants.ORGANELLE_MOVE_COST));
            }
        }

        return mutated;
    }
}
