namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    public static OrganelleDefinition Nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
    private readonly Direction direction;
    private readonly OrganelleDefinition[] allOrganelles;

    public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria, Direction direction = Direction.Neutral)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToArray();
        this.direction = direction;
    }

    public enum Direction
    {
        Front,
        Neutral,
        Rear,
    }

    public bool Repeatable => true;

    public static AddOrganelleAnywhere ThatUseCompound(Compound compound, Direction direction = Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses
            .Where(proc => proc.Process.Inputs.ContainsKey(compound)).Any(), direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(Compound compound,
        Direction direction = Direction.Neutral)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses
            .Where(proc => proc.Process.Outputs.ContainsKey(compound)).Any(), direction);
    }

    public static AddOrganelleAnywhere ThatConvertBetweenCompounds(Compound fromCompound, Compound toCompound,
        Direction direction = Direction.Neutral)
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

            OrganelleTemplate position;

            if (direction == Direction.Neutral)
            {
                position = CommonMutationFunctions
                    .GetRealisticPosition(organelle, newSpecies.Organelles, new Random());
            }
            else
            {
                var x = (int)(random.NextSingle() * 7 - 3);

                position = new OrganelleTemplate(organelle,
                    direction == Direction.Front ? new Hex(x, -100) : new Hex(x, 100),
                    direction == Direction.Front ? 0 : 3);
            }

            newSpecies.Organelles.Add(position);
            CommonMutationFunctions.AttachIslandHexes(newSpecies.Organelles);

            // If the new species is a eukaryote, mark this as such
            if (organelle == Nucleus)
            {
                newSpecies.IsBacteria = false;
            }

            mutated.Add(Tuple.Create(newSpecies, mp - organelle.MPCost));
        }

        return mutated;
    }
}
