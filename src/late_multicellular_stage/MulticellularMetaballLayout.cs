using Godot;

public class MulticellularMetaballLayout : MetaballLayout<MulticellularMetaball>
{
    /// <summary>
    ///   Repositions the bottom-most metaballs to touch the ground, and the center of all metaballs to be aligned
    ///   above 0,0
    /// </summary>
    public void RepositionToGround()
    {
        float lowestCoordinate = 0;
        var center = Vector3.Zero;

        foreach (var metaball in this)
        {
            center += metaball.Position;

            // Make the bottom edge of the lowest metaball touch the "ground"
            var bottom = metaball.Position.y - metaball.Size * 0.5f;

            if (bottom < lowestCoordinate)
                lowestCoordinate = bottom;
        }

        var adjustment = center / Count;
        adjustment.y = -lowestCoordinate;

        foreach (var metaball in this)
        {
            metaball.Position += adjustment;
        }
    }
}
