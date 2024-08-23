namespace AutoEvo;

using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Step that selects the best mutation for each species and registers all new species in results
/// </summary>
public class RegisterNewSpecies : IRunStep
{
    private readonly GameWorld world;
    private readonly HashSet<Species> oldSpecies;

    public RegisterNewSpecies(GameWorld world, HashSet<Species> oldSpecies)
    {
        this.world = world;
        this.oldSpecies = oldSpecies;
    }

    public int TotalSteps => 1;
    public bool CanRunConcurrently => false;

    public bool RunStep(RunResults results)
    {
        var currentSpecies = new HashSet<Species>();
        foreach (var patch in world.Map.Patches)
        {
            results.GetMicheForPatch(patch.Value).GetOccupants(currentSpecies);
        }

        var extinctSpecies = new HashSet<Species>();
        var handledSpecies = new HashSet<Species>();

        foreach (var species in oldSpecies)
        {
            if (!currentSpecies.Contains(species))
                extinctSpecies.Add(species);
        }

        var modifiedSpecies = results.GetPossibleSpeciesList();

        foreach (var extinct in extinctSpecies)
        {
            if (extinct.PlayerSpecies)
                continue;

            RunResults.PossibleSpecies? bestSpecies = null;
            var bestPopulation = 0.0;

            foreach (var species in modifiedSpecies)
            {
                if (species.ParentSpecies == extinct)
                {
                    var speciesPopulation = species.InitialPopulationInPatches.Sum(x => x.Value);
                    if (speciesPopulation > bestPopulation)
                    {
                        bestSpecies = species;
                        bestPopulation = speciesPopulation;
                    }
                }
            }

            if (bestSpecies != null)
            {
                results.AddMutationResultForSpecies(extinct, bestSpecies.Value.Species,
                    bestSpecies.Value.InitialPopulationInPatches);

                handledSpecies.Add(bestSpecies.Value.Species);
            }
        }

        foreach (var species in modifiedSpecies)
        {
            if (handledSpecies.Add(species.Species))
            {
                results.AddNewSpecies(species.Species, species.InitialPopulationInPatches, species.AddType,
                    species.ParentSpecies);
            }
        }

        return true;
    }
}
