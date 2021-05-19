namespace Thrive.src.auto_evo.simulation.niche
{
    public interface Niche
    {
        public float TotalEnergyAvailable();

        public float FitnessScore(MicrobeSpecies microbe);
    }
}
