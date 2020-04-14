namespace AutoEvo
{
    using System;

    /// <summary>
    ///   Data for a Species migration between two patches
    /// </summary>
    public class SpeciesMigration
    {
        public Patch From;
        public Patch To;
        public int Population;

        public SpeciesMigration(Patch from, Patch to, int population)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            Population = population;
        }
    }
}
