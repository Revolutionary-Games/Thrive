namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

public class RemoveOrganelle : IMutationStrategy<MicrobeSpecies>
{
    public static OrganelleDefinition Nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
    public Func<OrganelleDefinition, bool> Criteria;

    public RemoveOrganelle(Func<OrganelleDefinition, bool> criteria)
    {
        Criteria = criteria;
    }

    public bool Repeatable => true;

    public static RemoveOrganelle ThatUseCompound(Compound compound)
    {
        return new RemoveOrganelle(organelle => organelle.RunnableProcesses
            .Where(proc => proc.Process.Inputs.ContainsKey(compound)).Any());
    }

    public static RemoveOrganelle ThatCreateCompound(Compound compound)
    {
        return new RemoveOrganelle(organelle => organelle.RunnableProcesses
            .Where(proc => proc.Process.Outputs.ContainsKey(compound)).Any());
    }

    public List<Tuple<MicrobeSpecies, float>> MutationsOf(MicrobeSpecies baseSpecies, float mp)
    {
        if (mp < 10)
            return [];

        // TODO: Make this something passed in
        var random = new Random();

        // TODO: Move to constants
        const int removeOrganelleAttempts = 5;

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        var organelles = baseSpecies.Organelles.Where(x => Criteria(x.Definition))
            .OrderBy(_ => random.Next()).Take(removeOrganelleAttempts).ToList();

        if (organelles.Count <= 1)
            return [];

        var mutated = new List<Tuple<MicrobeSpecies, float>>();

        foreach (var organelle in organelles)
        {
            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            if (organelle.Definition == Nucleus)
            {
                if (baseSpecies.Organelles.Any(x => x.Definition.RequiresNucleus))
                    continue;

                newSpecies.IsBacteria = true;
            }

            newSpecies.Organelles.Clear();

            // Is this the best way to do this? Probably not, but this is how mutations.cs does is
            // and the other way outright did not work
            foreach (var parentOrganelle in baseSpecies.Organelles)
            {
                if (parentOrganelle == organelle)
                    continue;

                var newOrganelle = (OrganelleTemplate)parentOrganelle.Clone();

                // Copy the organelle
                if (newSpecies.Organelles.CanPlace(newOrganelle, workMemory1, workMemory2))
                    newSpecies.Organelles.AddFast(newOrganelle, workMemory1, workMemory2);
            }

            CommonMutationFunctions.AttachIslandHexes(newSpecies.Organelles);

            mutated.Add(Tuple.Create(newSpecies, mp - 10));
        }

        return mutated;
    }
}
