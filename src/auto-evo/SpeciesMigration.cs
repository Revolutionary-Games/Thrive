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
        public long Population;
        public bool OnlyVisual;

        public SpeciesMigration(Patch from, Patch to, long population, bool onlyVisual = false)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            Population = population;
            OnlyVisual = onlyVisual;
        }
    }
}
