namespace AutoEvo;

using System.Collections.Generic;

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
            results.GetModifiableMicheForPatch(patch.Value).GetOccupants(currentSpecies);
        }

        var extinctSpecies = new HashSet<Species>();
        var handledSpecies = new HashSet<Species>();

        foreach (var species in oldSpecies)
        {
            if (!currentSpecies.Contains(species))
                extinctSpecies.Add(species);
        }

        var modifiedSpecies = results.GetPossibleSpeciesList();
        var selectedParentMutations = new Dictionary<Species, Species>(ReferenceEqualityComparer.Instance);

        // Look through the extinct species and find their best modified variant to make it into that species' mutation
        foreach (var extinct in extinctSpecies)
        {
            if (extinct.PlayerSpecies)
                continue;

            RunResults.PossibleSpecies? bestSpecies = null;
            var bestPopulation = 0L;

            foreach (var species in modifiedSpecies)
            {
                if (species.ParentSpecies == extinct)
                {
                    var speciesPopulation = species.InitialPopulationInPatches.Value;
                    if (speciesPopulation > bestPopulation)
                    {
                        bestSpecies = species;
                        bestPopulation = speciesPopulation;
                    }
                }
            }

            if (bestSpecies != null)
            {
                // If the mutant species is in the same Stage as the parent, it can be applied as a mutation
                if (bestSpecies.Value.Species.GetType() == extinct.GetType())
                {
                    results.AddMutationResultForSpecies(extinct, bestSpecies.Value.Species,
                        bestSpecies.Value.InitialPopulationInPatches);

                    handledSpecies.Add(bestSpecies.Value.Species);
                    selectedParentMutations.Add(bestSpecies.Value.Species, extinct);

                    continue;
                }

                // If the mutant transitions to a new Stage, it cannot be applied to the parent species as a mutation.
                // If it is the best mutant, it will still inherit the parent's population, but the parent species
                // will simply go extinct.
                if (handledSpecies.Add(bestSpecies.Value.Species))
                {
                    results.AddNewSpecies(bestSpecies.Value.Species,
                        bestSpecies.Value.InitialPopulationInPatches, bestSpecies.Value.AddType,
                        extinct, true);
                }
            }
        }

        // Add other mutations as full new species (that split off)
        foreach (var species in modifiedSpecies)
        {
            if (handledSpecies.Add(species.Species))
            {
                results.AddNewSpecies(species.Species, species.InitialPopulationInPatches, species.AddType,
                    species.ParentSpecies, false);
            }
        }

        // Decide every candidate's destination before changing the occupants used for extinction detection.
        // Only the selected same-stage candidate becomes its parent; other candidates keep their own identity.
        if (selectedParentMutations.Count > 0)
        {
            var leaves = new List<Miche>();
            foreach (var patch in world.Map.Patches)
            {
                leaves.Clear();
                results.GetModifiableMicheForPatch(patch.Value).GetLeafNodes(leaves);
                foreach (var leaf in leaves)
                {
                    if (leaf.Occupant != null && selectedParentMutations.TryGetValue(leaf.Occupant, out var parent))
                        leaf.Occupant = parent;
                }
            }
        }

        return true;
    }
}
