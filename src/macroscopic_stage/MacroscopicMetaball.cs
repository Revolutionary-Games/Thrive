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
    public override Color Colour => CellType.Colour;

    public override bool MatchesDefinition(Metaball other)
    {
        if (other is MulticellularMetaball asMulticellular)
        {
            return CellType == asMulticellular.CellType;
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((MulticellularMetaball)obj);
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

    public override int GetHashCode()
    {
        return CellType.GetHashCode() * 29 ^ base.GetHashCode();
    }

    protected bool Equals(MulticellularMetaball other)
    {
        // This seems to cause infinite recursion, so this is not done for now and parents need to equal references
        // and not values
        // if (!ReferenceEquals(Parent, other.Parent))
        // {
        //     if (ReferenceEquals(Parent, null) && !ReferenceEquals(other.Parent, null))
        //         return false;
        //
        //     if (!ReferenceEquals(Parent, null) && ReferenceEquals(other.Parent, null))
        //         return false;
        //
        //     if (!Parent!.Equals(other.Parent))
        //         return false;
        // }

        if (!ReferenceEquals(Parent, other.Parent))
            return false;

        return CellType.Equals(other.CellType) && Position == other.Position;
    }
}
