namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
    {
        public Func<OrganelleDefinition, bool> Criteria;
        private Direction direction;

        public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria, Direction direction = Direction.NEUTRAL)
        {
            Criteria = criteria;
            this.direction = direction;
        }

        public enum Direction
        {
            FRONT,
            NEUTRAL,
            REAR,
        }

        public static AddOrganelleAnywhere ThatUseCompound(Compound compound, Direction direction = Direction.NEUTRAL)
        {
            return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses
                .Where(proc => proc.Process.Inputs.ContainsKey(compound)).Any(), direction);
        }

        public static AddOrganelleAnywhere ThatCreateCompound(Compound compound,
            Direction direction = Direction.NEUTRAL)
        {
            var matches = SimulationParameters.Instance.GetAllOrganelles()
                .Where(organelle => organelle.RunnableProcesses
                .Where(proc => proc.Process.Outputs.ContainsKey(compound)).Any()).Count();

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

                OrganelleTemplate? position = null;

                if (direction == Direction.NEUTRAL)
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

                    position = choices.OrderBy(x => direction == Direction.FRONT ? x.Position.R : -x.Position.R)
                        .First();
                }

                newSpecies.Organelles.Add(position);

                retval.Add(newSpecies);
            }

            return retval;
        }
    }
}
