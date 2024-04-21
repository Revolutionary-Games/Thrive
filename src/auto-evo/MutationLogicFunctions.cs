namespace AutoEvo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class MutationLogicFunctions
{
    public static void NameNewMicrobeSpecies(MicrobeSpecies newSpecies, MicrobeSpecies parentSpecies)
    {
        // If for some silly reason the species are the same don't rename
        if (newSpecies == parentSpecies)
        {
            return;
        }

        if (MicrobeSpeciesIsNewGenus(newSpecies, parentSpecies))
        {
            newSpecies.Genus = SimulationParameters.Instance.NameGenerator.GenerateNameSection();
        }
        else
        {
            newSpecies.Genus = parentSpecies.Genus;
        }

        newSpecies.Epithet = SimulationParameters.Instance.NameGenerator.GenerateNameSection();
    }

    private static bool MicrobeSpeciesIsNewGenus(MicrobeSpecies species1, MicrobeSpecies species2)
    {
        var species1UniqueOrganelles = species1.Organelles.Select(o => o.Definition).ToHashSet();
        var species2UniqueOrganelles = species2.Organelles.Select(o => o.Definition).ToHashSet();

        return species1UniqueOrganelles.Union(species2UniqueOrganelles).Count()
            - species1UniqueOrganelles.Intersect(species2UniqueOrganelles).Count()
            >= Constants.DIFFERENCES_FOR_GENUS_SPLIT;
    }
}
