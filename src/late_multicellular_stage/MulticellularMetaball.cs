using System;
using Godot;
using Newtonsoft.Json;

[UseThriveConverter]
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

    [JsonIgnore]
    public override Color Color => CellType.Colour;

    public object Clone()
    {
        throw new NotImplementedException();
    }
}
