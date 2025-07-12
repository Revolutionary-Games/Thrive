namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using static CommonMutationFunctions;

public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    private readonly Direction direction;
    private readonly OrganelleDefinition[] allOrganelles;

    public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria, Direction direction = Direction.Neutral)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).Where(x => x.AutoEvoCanPlace)
            .ToArray();

        this.direction = direction;
    }

    public bool Repeatable => true;

    // Formatter and inspect code disagree here
    // ReSharper disable InvokeAsExtensionMethod
    public static AddOrganelleAnywhere ThatUseCompound(CompoundDefinition compound,
        Direction direction = Direction.Neutral)
    {
        return new AddOrganelleAnywhere(
            organelle => Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Inputs.ContainsKey(compound)),
            direction);
    }

    public static AddOrganelleAnywhere ThatUseCompound(Compound compound, Direction direction
        = Direction.Neutral)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatUseCompound(compoundResolved, direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(CompoundDefinition compound,
        Direction direction = Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle =>
                Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Outputs.ContainsKey(compound)),
            direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(Compound compound,
        Direction direction = Direction.Neutral)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatCreateCompound(compoundResolved, direction);
    }

    public static AddOrganelleAnywhere ThatConvertBetweenCompounds(CompoundDefinition fromCompound,
        CompoundDefinition toCompound,
        Direction direction = Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => Enumerable.Any(organelle.RunnableProcesses, proc =>
            proc.Process.Inputs.ContainsKey(fromCompound) &&
            proc.Process.Outputs.ContainsKey(toCompound)), direction);
    }

    // ReSharper restore InvokeAsExtensionMethod

    public static AddOrganelleAnywhere ThatConvertBetweenCompounds(Compound fromCompound, Compound toCompound,
        Direction direction = Direction.Neutral)
    {
        var fromCompoundResolved = SimulationParameters.GetCompound(fromCompound);
        var toCompoundResolved = SimulationParameters.GetCompound(toCompound);

        return ThatConvertBetweenCompounds(fromCompoundResolved, toCompoundResolved, direction);
    }

    public List<Mutant>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        // If a cheaper organelle gets added, this will need to be updated
        if (mp < 20)
            return null;

        // TODO: would the following be more efficient?
        // var organelles = allOrganelles.ToList();
        // organelles.Shuffle(random);
        // organelles.RemoveRange(Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS,
        //     organelles.Count - Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS);

        var organelles = allOrganelles.OrderBy(_ => random.Next())
            .Take(Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS);

        var mutated = new List<Mutant>();

        // TODO: reuse this memory somehow
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var workMemory3 = new HashSet<Hex>();

        foreach (var organelle in organelles)
        {
            if (organelle.MPCost > mp)
                continue;

            // Important to not accidentally add non-LAWK organelles in a LAWK game
            if (!organelle.LAWK && lawk)
                continue;

            if (organelle.RequiresNucleus && baseSpecies.IsBacteria)
                continue;

            if (organelle.Unique && baseSpecies.Organelles.Select(x => x.Definition).Contains(organelle))
                continue;

            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            // In the rare case that adding the organelle fails, this can skip adding it to be tested as the species
            // is not any different
            if (AddOrganelle(organelle, direction, newSpecies, workMemory1, workMemory2,
                    workMemory3, random))
            {
                mutated.Add(new Mutant(newSpecies, mp - organelle.MPCost));
            }
        }

        return mutated;
    }
}
