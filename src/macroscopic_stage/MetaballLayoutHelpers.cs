using System.Collections.Generic;

public static class MetaballLayoutHelpers
{
    public static ulong CalculateLayoutHash(IReadOnlyList<IReadonlyMacroscopicMetaball> layout)
    {
        ulong value = 1610612741UL;

        int count = layout.Count;

        for (int i = 0; i < count; ++i)
        {
            var metaball = layout[i];

            value += (ulong)(metaball.Position.X.GetHashCode() ^ metaball.Position.Y.GetHashCode()
                ^ metaball.Position.Z.GetHashCode() ^ metaball.Size.GetHashCode());

            value ^= metaball.Colour.GetVisualHashCode();
        }

        return value;
    }
}
