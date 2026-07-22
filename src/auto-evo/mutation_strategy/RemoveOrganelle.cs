namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using static CommonMutationFunctions;

public class RemoveOrganelle : IMutationStrategy<Species>
{
    public static OrganelleDefinition Nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
    public Func<OrganelleDefinition, bool> Criteria;

    public RemoveOrganelle(Func<OrganelleDefinition, bool> criteria)
    {
        Criteria = criteria;
    }

    public bool Repeatable => true;

    // Formatter and inspect code disagree here
    // ReSharper disable InvokeAsExtensionMethod
    public static RemoveOrganelle ThatUseCompound(CompoundDefinition compound)
    {
        return new RemoveOrganelle(organelle =>
            Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Inputs.ContainsKey(compound)));
    }

    public static RemoveOrganelle ThatUseCompound(Compound compound)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatUseCompound(compoundResolved);
    }

    public static RemoveOrganelle ThatCreateCompound(CompoundDefinition compound)
    {
        return new RemoveOrganelle(organelle =>
            Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Outputs.ContainsKey(compound)));
    }

    public static RemoveOrganelle ThatCreateCompound(Compound compound)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatCreateCompound(compoundResolved);
    }

    // ReSharper restore InvokeAsExtensionMethod

    public List<Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (baseSpecies is MicrobeSpecies baseMicrobeSpecies)
        {
            if (mp < Constants.ORGANELLE_REMOVE_COST)
                return null;

            if (baseMicrobeSpecies.Organelles.Count <= 1)
                return null;

            var organelles = baseMicrobeSpecies.Organelles.Where(x => Criteria(x.Definition))
                .OrderBy(_ => random.Next()).Take(Constants.AUTO_EVO_ORGANELLE_REMOVE_ATTEMPTS);

            List<Mutant>? mutated = null;

            MutationWorkMemory? workMemory = null;

            foreach (var organelle in organelles)
            {
                // The player cannot remove the nucleus, so Auto-Evo should not be able to either
                if (organelle.Definition == Nucleus)
                    continue;

                // Don't clone organelles as we want to do those ourselves
                var newSpecies = baseMicrobeSpecies.Clone(false);

                workMemory ??= new MutationWorkMemory();

                // Is this the best way to do this? Probably not, but this is how mutations.cs does is
                // and the other way outright did not work
                // This is now slightly improved - hhyyrylainen
                var baseOrganelles = baseMicrobeSpecies.Organelles.Organelles;
                var count = baseMicrobeSpecies.Organelles.Count;

                for (var i = 0; i < count; ++i)
                {
                    var parentOrganelle = baseOrganelles[i];

                    if (parentOrganelle == organelle)
                        continue;

                    // Copy the organelle
                    var newOrganelle = parentOrganelle.Clone();
                    baseMicrobeSpecies.Organelles.AddIfPossible(newOrganelle, workMemory.WorkingMemory1,
                        workMemory.WorkingMemory2);
                }

                AttachIslandHexes(baseMicrobeSpecies.Organelles, workMemory);

                mutated ??= new List<Mutant>();
                mutated.Add(new Mutant(newSpecies, mp - Constants.ORGANELLE_REMOVE_COST));
            }

            return mutated;
        }

        if (baseSpecies is MulticellularSpecies baseMulticellularSpecies)
        {
            var mpCost = Constants.ORGANELLE_REMOVE_COST * Constants.MULTICELLULAR_EDITOR_COST_FACTOR;
            if (mp < mpCost)
                return null;

            List<Mutant>? mutated = null;

            var cellTypeCount = baseMulticellularSpecies.CellTypes.Count;

            for (var i = 0; i < cellTypeCount; ++i)
            {
                var baseCellType = baseMulticellularSpecies.ModifiableCellTypes[i];
                if (baseCellType.Organelles.Count <= 1)
                    return null;

                var organelles = baseCellType.Organelles.Where(x => Criteria(x.Definition))
                    .OrderBy(_ => random.Next()).Take(Constants.AUTO_EVO_ORGANELLE_REMOVE_ATTEMPTS);

                MutationWorkMemory? workMemory = null;

                foreach (var organelle in organelles)
                {
                    // The player cannot remove the nucleus, so Auto-Evo should not be able to either
                    if (organelle.Definition == Nucleus)
                        continue;

                    // The Binding Agent cannot be removed in the Multicellular Stage
                    if (organelle.Definition.HasBindingFeature)
                        continue;

                    // Don't clone organelles as we want to do those ourselves
                    var newSpecies = (MulticellularSpecies)baseMulticellularSpecies.Clone();
                    var newCellTypeOrganelles =
                        newSpecies.ModifiableCellTypes[i].ModifiableOrganelles;
                    newCellTypeOrganelles.Clear();

                    workMemory ??= new MutationWorkMemory();

                    // Is this the best way to do this?
                    var baseOrganelles = baseCellType.ModifiableOrganelles;
                    var organelleCount = baseCellType.Organelles.Count;

                    for (var j = 0; j < organelleCount; ++j)
                    {
                        var parentOrganelle = baseOrganelles[j];

                        if (parentOrganelle == organelle)
                            continue;

                        // Copy the organelle
                        var newOrganelle = parentOrganelle.Clone();
                        newCellTypeOrganelles.AddIfPossible(newOrganelle, workMemory.WorkingMemory1,
                            workMemory.WorkingMemory2);
                    }

                    AttachIslandHexes(newCellTypeOrganelles, workMemory);

                    mutated ??= new List<Mutant>();
                    mutated.Add(new Mutant(newSpecies, mp - mpCost));
                }
            }

            return mutated;
        }

        return null;
    }
}
