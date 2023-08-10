namespace Components
{
    using System.Collections.Generic;

    /// <summary>
    ///   General info about the reproduction status of a creature
    /// </summary>
    public struct ReproductionStatus
    {
        public Dictionary<Compound, float>? MissingCompoundsForBaseReproduction;

        public bool ReadyToReproduce;
    }
}
