using System;
using Newtonsoft.Json;

public class MulticellularMetaball : Metaball, ICloneable
{
    public MulticellularMetaball(CellType cellType)
    {
        CellType = cellType;
    }

    /// <summary>
    ///   The cell type this metaball consists of
    /// </summary>
    [JsonProperty]
    public CellType CellType { get; private set; }

    public object Clone()
    {
        throw new NotImplementedException();
    }
}
