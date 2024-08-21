namespace AutoEvo;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    private readonly CommonMutationFunctions.Direction direction;
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;

    public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria, CommonMutationFunctions.Direction direction
        = CommonMutationFunctions.Direction.Neutral)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        this.direction = direction;
    }

    public bool Repeatable => true;

    public static AddOrganelleAnywhere ThatUseCompound(Compound compound, CommonMutationFunctions.Direction direction
        = CommonMutationFunctions.Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses
            .Any(proc => proc.Process.Inputs.ContainsKey(compound)), direction);
    }

    public static AddOrganelleAnywhere ThatUseCompound(string compoundName, CommonMutationFunctions.Direction direction
        = CommonMutationFunctions.Direction.Neutral)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);

        return ThatUseCompound(compound, direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(Compound compound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses
            .Any(proc => proc.Process.Outputs.ContainsKey(compound)), direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(string compoundName,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);

        return ThatCreateCompound(compound, direction);
    }

    public static AddOrganelleAnywhere ThatConvertBetweenCompounds(Compound fromCompound, Compound toCompound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses
            .Any(proc => proc.Process.Inputs.ContainsKey(fromCompound) &&
                proc.Process.Outputs.ContainsKey(toCompound)), direction);
    }

    public static AddOrganelleAnywhere ThatConvertBetweenCompounds(string fromCompoundName, string toCompoundName,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        var fromCompound = SimulationParameters.Instance.GetCompound(fromCompoundName);
        var toCompound = SimulationParameters.Instance.GetCompound(toCompoundName);

        return ThatConvertBetweenCompounds(fromCompound, toCompound, direction);
    }

    public List<Tuple<MicrobeSpecies, float>>? MutationsOf(MicrobeSpecies baseSpecies, float mp)
    {
        // If a cheaper organelle gets added this will need to be updated
        if (mp < 20)
            return null;

        // TODO: Make this something passed in
        var random = new Random();

        var organelles = allOrganelles.OrderBy(_ => random.Next())
            .Take(Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS).ToList();

        var mutated = new List<Tuple<MicrobeSpecies, float>>();

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var organelle in organelles)
        {
            if (organelle.MPCost > mp)
                continue;

            if (organelle.RequiresNucleus && baseSpecies.IsBacteria)
                continue;

            if (organelle.Unique && baseSpecies.Organelles.Select(x => x.Definition).Contains(organelle))
                continue;

            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            CommonMutationFunctions.AddOrganelle(organelle, direction, newSpecies, workMemory1, workMemory2, random);

            mutated.Add(Tuple.Create(newSpecies, mp - organelle.MPCost));
        }

        return mutated;
    }
}
