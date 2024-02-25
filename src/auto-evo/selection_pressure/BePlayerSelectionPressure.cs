namespace AutoEvo
{
    using System.Collections.Generic;

    public class BePlayerSelectionPressure : SelectionPressure
    {
        private readonly float weight;
        public BePlayerSelectionPressure(float weight) : base(
            weight,
            new List<IMutationStrategy<MicrobeSpecies>>())
        {
            EnergyProvided = 1000;
            this.weight = weight;
        }

        public override float Score(MicrobeSpecies species, SimulationCache cache)
        {
            return species.PlayerSpecies ? 1.0f * weight : -1.0f;
        }
    }
}
