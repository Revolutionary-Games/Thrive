namespace Components
{
    using System.Collections.Generic;
    using DefaultEcs;

    /// <summary>
    ///   Keeps track of multicellular growth data
    /// </summary>
    public struct MulticellularGrowth
    {
        public Dictionary<CellType, Entity>? GrownCells;
    }
}
