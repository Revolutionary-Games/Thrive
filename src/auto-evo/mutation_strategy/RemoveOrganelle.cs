namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

public class RemoveOrganelle : IMutationStrategy<MicrobeSpecies>
{
    public Func<OrganelleDefinition, bool> Criteria;

    public RemoveOrganelle(Func<OrganelleDefinition, bool> criteria)
    {
        Criteria = criteria;
    }

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

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, MutationLibrary partList)
    {
        // TODO: Make this something passed in
        var random = new Random();

        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        var organelles = newSpecies.Organelles.ToList().Where(x => Criteria(x.Definition)).ToList();

        if (organelles.Count <= 1)
            return [newSpecies];

        newSpecies.Organelles.RemoveHexAt(organelles.ElementAt(random.Next(0, organelles.Count)).Position, []);

        CommonMutationFunctions.AttachIslandHexes(newSpecies.Organelles);

        return [newSpecies];
    }
}
