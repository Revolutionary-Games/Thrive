namespace AutoEvo;

using System;
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

            Tuple<Species, IEnumerable<KeyValuePair<Patch, long>>, RunResults.NewSpeciesType, Species>?
                bestSpecies = null;
            var bestPopulation = 0.0;

            foreach (var species in modifiedSpecies)
            {
                if (species.Item4 == extinct)
                {
                    var speciesPopulation = species.Item2.Sum(x => x.Value);
                    if (speciesPopulation > bestPopulation)
                    {
                        bestSpecies = species;
                        bestPopulation = speciesPopulation;
                    }
                }
            }

            if (bestSpecies != null)
            {
                results.AddMutationResultForSpecies(extinct, bestSpecies.Item1, bestSpecies.Item2);
                handledSpecies.Add(bestSpecies.Item1);
            }
        }

        foreach (var species in modifiedSpecies)
        {
            if (handledSpecies.Add(species.Item1))
            {
                results.AddNewSpecies(species.Item1, species.Item2, species.Item3, species.Item4);
            }
        }

        return true;
    }
}
