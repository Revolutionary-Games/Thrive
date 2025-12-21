namespace AutoEvo;

using System;
using System.Linq;
using Godot;

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

        newSpecies.Epithet = SimulationParameters.Instance.NameGenerator.GenerateNameSection(null, true);
    }

    public static void ColorNewMicrobeSpecies(Random random, MicrobeSpecies newSpecies,
        MicrobeSpecies? parentSpecies = null)
    {
        // If for some silly reason the species are the same don't recolor
        if (parentSpecies != null && newSpecies == parentSpecies)
        {
            return;
        }

        var oldColour = newSpecies.SpeciesColour;

        float redShift;
        float greenShift;
        float blueShift;

        // make sure that species mutated from player have visibly different color
        if (parentSpecies?.PlayerSpecies == true)
        {
            redShift = random.Next(0.25f, 0.75f);
            greenShift = random.Next(0.25f, 0.75f);
            blueShift = random.Next(0.25f, 0.75f);

            var red = (oldColour.R + redShift) % 1.0f;
            var green = (oldColour.G + greenShift) % 1.0f;
            var blue = (oldColour.B + blueShift) % 1.0f;

            newSpecies.SpeciesColour = new Color(red, green, blue);
        }
        else
        {
            redShift = (float)(random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;
            greenShift = (float)(random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;
            blueShift = (float)(random.NextDouble() - 0.5f) * Constants.AUTO_EVO_COLOR_CHANGE_MAX_STEP;

            newSpecies.SpeciesColour = new Color(Math.Clamp(oldColour.R + redShift, 0, 1),
                Math.Clamp(oldColour.G + greenShift, 0, 1),
                Math.Clamp(oldColour.B + blueShift, 0, 1));
        }
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
