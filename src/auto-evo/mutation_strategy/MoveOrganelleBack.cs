using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using static CommonMutationFunctions;

internal class MoveOrganelleBack : IMutationStrategy<MicrobeSpecies>
{
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;

    public MoveOrganelleBack(Func<OrganelleDefinition, bool> criteria)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
    }

    public bool Repeatable => true;

    public List<Mutant>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (mp < Constants.ORGANELLE_MOVE_COST)
            return null;

        var mutated = new List<Mutant>();

        // TODO: try to avoid these temporary list allocations
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var workMemory3 = new HashSet<Hex>();

        foreach (OrganelleTemplate organelle in baseSpecies.Organelles.Where(x => allOrganelles.Contains(x.Definition)))
        {
            var newSpecies = baseSpecies.Clone(false);

            var baseOrganelles = baseSpecies.Organelles.Organelles;
            var count = baseOrganelles.Count;

            for (var i = 0; i < count; ++i)
            {
                var existingOrganelle = baseOrganelles[i];

                if (ReferenceEquals(existingOrganelle, organelle))
                    continue;

                newSpecies.Organelles.AddAutoEvoAttemptOrganelle(existingOrganelle.Clone());
            }

            if (AddOrganelle(organelle.Definition, Direction.Rear, newSpecies, workMemory1, workMemory2, workMemory3,
                    random))
            {
                // Add mutation attempt only if was able to place the organelle
                // TODO: maybe this should add the attempt anyway as this may act as a separate remove organelle step
                // for things that cannot be moved?
                mutated.Add(new Mutant(newSpecies, mp - Constants.ORGANELLE_MOVE_COST));
            }
        }

        return mutated;
    }
}
