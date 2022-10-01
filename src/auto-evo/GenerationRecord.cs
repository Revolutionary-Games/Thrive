namespace AutoEvo
{
    using System.Collections.Generic;

    public class GenerationRecord
    {
        public int Generation;
        public double TimeElapsed;

        public RunResults AutoEvoResult = null!;

        public Dictionary<uint, Species> AllSpecies = null!;
    }
}