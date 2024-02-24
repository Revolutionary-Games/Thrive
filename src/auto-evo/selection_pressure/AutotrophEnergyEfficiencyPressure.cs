namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Systems;

    public class AutotrophEnergyEfficiencyPressure : SelectionPressure
    {
        public Patch Patch;
        public Compound Compound;
        private readonly float weight;

        public AutotrophEnergyEfficiencyPressure(Patch patch, Compound compound, float weight) : base(
            weight,
            new List<IMutationStrategy<MicrobeSpecies>>
            {
                new AddOrganelleAnywhere(organelle => organelle.Name.Equals("rusticyanin")),
                AddOrganelleAnywhere.ThatUseCompound(compound),
                new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("cellulose")),
            })
        {
            Patch = patch;
            Compound = compound;
            EnergyProvided = 40000;
            this.weight = weight;
        }

        public override float Score(MicrobeSpecies species, SimulationCache cache)
        {
            var score = 0.1f;

            // We check generation from all the processes of the cell../
            foreach (var organelle in species.Organelles)
            {
                GD.Print("hallo3");
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    GD.Print("hallo2");
                    // ... that uses the given compound (regardless of usage)
                    if (process.Process.Inputs.TryGetValue(Compound, out var inputAmount))
                    {
                        GD.Print("hallo");
                        var processEfficiency = ProcessSystem.CalculateProcessMaximumSpeed(process, Patch.Biome, CompoundAmountType.Average).Efficiency;

                        score += inputAmount * processEfficiency;
                    }
                }
            }

            return score * weight;
        }
    }
}
