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
            .Where(proc => proc.Process.Inputs.ContainsKey(compound)).Any(), direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(Compound compound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses
            .Where(proc => proc.Process.Outputs.ContainsKey(compound)).Any(), direction);
    }

    public static AddOrganelleAnywhere ThatConvertBetweenCompounds(Compound fromCompound, Compound toCompound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses
            .Where(proc => proc.Process.Inputs.ContainsKey(fromCompound) &&
                proc.Process.Outputs.ContainsKey(toCompound)).Any(), direction);
    }

    public List<Tuple<MicrobeSpecies, float>> MutationsOf(MicrobeSpecies baseSpecies, float mp)
    {
        // If a cheaper organelle gets added this will need to be updated
        if (mp < 20)
            return [];

        // TODO: Make this something passed in
        var random = new Random();

        var organelles = allOrganelles.OrderBy(_ => random.Next())
            .Take(Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS).ToList();

        var mutated = new List<Tuple<MicrobeSpecies, float>>();

        foreach (var organelle in organelles)
        {
            if (organelle.MPCost > mp)
                continue;

            if (organelle.RequiresNucleus && baseSpecies.IsBacteria)
                continue;

            if (organelle.Unique && baseSpecies.Organelles.Select(x => x.Definition).Contains(organelle))
                continue;

            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            CommonMutationFunctions.AddOrganelle(organelle, direction, newSpecies, random);

            mutated.Add(Tuple.Create(newSpecies, mp - organelle.MPCost));
        }

        return mutated;
    }
}
