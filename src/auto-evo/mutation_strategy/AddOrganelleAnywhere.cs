namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Adds a random, valid organelle to a valid position. Doesn't place multicellular or later organelles.
/// </summary>
public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    private readonly CommonMutationFunctions.Direction direction;
    private readonly OrganelleDefinition[] allOrganelles;

    public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria, CommonMutationFunctions.Direction direction
        = CommonMutationFunctions.Direction.Neutral)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).Where(IsOrganelleValid)
            .ToArray();

        this.direction = direction;
    }

    public bool Repeatable => true;

    // Formatter and inspect code disagree here
    // ReSharper disable InvokeAsExtensionMethod
    public static AddOrganelleAnywhere ThatUseCompound(CompoundDefinition compound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        return new AddOrganelleAnywhere(
            organelle => Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Inputs.ContainsKey(compound)),
            direction);
    }

    public static AddOrganelleAnywhere ThatUseCompound(Compound compound, CommonMutationFunctions.Direction direction
        = CommonMutationFunctions.Direction.Neutral)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatUseCompound(compoundResolved, direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(CompoundDefinition compound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle =>
                Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Outputs.ContainsKey(compound)),
            direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(Compound compound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatCreateCompound(compoundResolved, direction);
    }

    public static AddOrganelleAnywhere ThatConvertBetweenCompounds(CompoundDefinition fromCompound,
        CompoundDefinition toCompound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => Enumerable.Any(organelle.RunnableProcesses, proc =>
            proc.Process.Inputs.ContainsKey(fromCompound) &&
            proc.Process.Outputs.ContainsKey(toCompound)), direction);
    }

    // ReSharper restore InvokeAsExtensionMethod

    public static AddOrganelleAnywhere ThatConvertBetweenCompounds(Compound fromCompound, Compound toCompound,
        CommonMutationFunctions.Direction direction = CommonMutationFunctions.Direction.Neutral)
    {
        var fromCompoundResolved = SimulationParameters.GetCompound(fromCompound);
        var toCompoundResolved = SimulationParameters.GetCompound(toCompound);

        return ThatConvertBetweenCompounds(fromCompoundResolved, toCompoundResolved, direction);
    }

    public List<Tuple<MicrobeSpecies, double>>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
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

        var mutated = new List<Tuple<MicrobeSpecies, double>>();

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
            if (CommonMutationFunctions.AddOrganelle(organelle, direction, newSpecies, workMemory1, workMemory2,
                    workMemory3, random))
            {
                mutated.Add(Tuple.Create(newSpecies, mp - organelle.MPCost));
            }
        }

        return mutated;
    }

    /// <summary>
    ///   Macroscopic, multicellular, and non-placeable organelles are invalid and so won't be considered.
    /// </summary>
    private static bool IsOrganelleValid(OrganelleDefinition organelle)
    {
        // TODO: allow placement of multicellular organelles in the appropriate stages.
        return organelle.AutoEvoCanPlace &&
            organelle.EditorButtonGroup != OrganelleDefinition.OrganelleGroup.Multicellular &&
            organelle.EditorButtonGroup != OrganelleDefinition.OrganelleGroup.Macroscopic;
    }
}
