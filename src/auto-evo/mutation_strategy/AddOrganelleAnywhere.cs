namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    public Func<OrganelleDefinition, bool> Criteria;
    private Direction direction;

    public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria, Direction direction = Direction.Neutral)
    {
        Criteria = criteria;
        this.direction = direction;
    }

    public enum Direction
    {
        Front,
        Neutral,
        Rear,
    }

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

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, MutationLibrary partList)
    {
        var viableOrganelles = partList.GetAllOrganelles().Where(x => Criteria(x));

        var retval = new List<MicrobeSpecies>();

        foreach (var organelle in viableOrganelles)
        {
            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            OrganelleTemplate position;

            if (direction == Direction.Neutral)
            {
                position = CommonMutationFunctions
                    .GetRealisticPosition(organelle, newSpecies.Organelles, new Random());
            }
            else
            {
                var choices = new List<OrganelleTemplate>
                {
                    CommonMutationFunctions.GetRealisticPosition(organelle, newSpecies.Organelles, new Random()),
                    CommonMutationFunctions.GetRealisticPosition(organelle, newSpecies.Organelles, new Random()),
                    CommonMutationFunctions.GetRealisticPosition(organelle, newSpecies.Organelles, new Random()),
                    CommonMutationFunctions.GetRealisticPosition(organelle, newSpecies.Organelles, new Random()),
                    CommonMutationFunctions.GetRealisticPosition(organelle, newSpecies.Organelles, new Random()),
                };

                position = choices.OrderBy(x => direction == Direction.Front ? x.Position.R : -x.Position.R)
                    .First();
            }

            newSpecies.Organelles.Add(position);

            retval.Add(newSpecies);
        }

        return retval;
    }
}
