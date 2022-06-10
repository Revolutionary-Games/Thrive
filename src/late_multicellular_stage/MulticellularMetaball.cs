using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

[UseThriveConverter]
public class MulticellularMetaball : Metaball
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

    public override bool MatchesDefinition(Metaball other)
    {
        if (other is MulticellularMetaball asMulticellular)
        {
            return CellType == asMulticellular.CellType;
        }

        return false;
    }

    /// <summary>
    ///   Clones this metaball while keeping the parent references intact.
    /// </summary>
    /// <param name="oldToNewMapping">
    ///   Where to find new reference to parent nodes. This will also add the newly cloned object here.
    /// </param>
    /// <returns>The clone of this</returns>
    public MulticellularMetaball Clone(Dictionary<Metaball, MulticellularMetaball> oldToNewMapping)
    {
        var clone = new MulticellularMetaball(CellType)
        {
            Position = Position,
            Parent = Parent,
            Size = Size,
        };

        if (Parent != null)
        {
            if (oldToNewMapping.TryGetValue(Parent, out var newParent))
            {
                clone.Parent = newParent;
            }
        }

        oldToNewMapping[this] = clone;

        return clone;
    }
}
