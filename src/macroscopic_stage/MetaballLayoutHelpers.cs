using System.Collections.Generic;

public static class MetaballLayoutHelpers
{
    public static ulong CalculateLayoutHash(IReadOnlyCollection<MacroscopicMetaball> layout)
    {
        ulong value = 1610612741UL;

        foreach (var metaball in layout)
        {
            value ^= (ulong)((metaball.Position.X.GetHashCode() + metaball.Position.Y.GetHashCode()
                + metaball.Position.Z.GetHashCode()) ^ metaball.Size.GetHashCode() ^ metaball.Colour.GetHashCode());
        }

        return value;
    }
}
