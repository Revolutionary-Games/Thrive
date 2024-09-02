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
        return new RemoveOrganelle(organelle =>
            organelle.RunnableProcesses.Any(proc => proc.Process.Inputs.ContainsKey(compound)));
    }

    public static RemoveOrganelle ThatUseCompound(string compoundName)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);

        return ThatUseCompound(compound);
    }

    public static RemoveOrganelle ThatCreateCompound(Compound compound)
    {
        return new RemoveOrganelle(organelle =>
            organelle.RunnableProcesses.Any(proc => proc.Process.Outputs.ContainsKey(compound)));
    }

    public static RemoveOrganelle ThatCreateCompound(string compoundName)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);

        return ThatCreateCompound(compound);
    }

    public List<Tuple<MicrobeSpecies, float>>? MutationsOf(MicrobeSpecies baseSpecies, float mp, bool lawk,
        Random random)
    {
        if (mp < Constants.ORGANELLE_REMOVE_COST)
            return null;

        var organelles = baseSpecies.Organelles.Where(x => Criteria(x.Definition))
            .OrderBy(_ => random.Next()).Take(Constants.AUTO_EVO_ORGANELLE_REMOVE_ATTEMPTS);

        List<Tuple<MicrobeSpecies, float>>? mutated = null;

        MutationWorkMemory? workMemory = null;

        foreach (var organelle in organelles)
        {
            if (organelle.Definition == Nucleus)
                continue;

            // Don't clone organelles as we want to do those ourselves
            var newSpecies = baseSpecies.Clone(false);

            workMemory ??= new MutationWorkMemory();

            // Is this the best way to do this? Probably not, but this is how mutations.cs does is
            // and the other way outright did not work
            // This is now slightly improved - hhyyrylainen
            var baseOrganelles = baseSpecies.Organelles.Organelles;
            var count = baseSpecies.Organelles.Count;

            for (var i = 0; i < count; ++i)
            {
                var parentOrganelle = baseOrganelles[i];

                if (parentOrganelle == organelle)
                    continue;

                // Copy the organelle
                var newOrganelle = (OrganelleTemplate)parentOrganelle.Clone();
                newSpecies.Organelles.AddIfPossible(newOrganelle, workMemory.WorkingMemory1, workMemory.WorkingMemory2);
            }

            CommonMutationFunctions.AttachIslandHexes(newSpecies.Organelles, workMemory);

            mutated ??= new List<Tuple<MicrobeSpecies, float>>();
            mutated.Add(Tuple.Create(newSpecies, mp - Constants.ORGANELLE_REMOVE_COST));
        }

        return mutated;
    }
}
