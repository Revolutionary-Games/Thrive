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

            if (metaball.Position.y < lowestCoordinate)
                lowestCoordinate = metaball.Position.y;
        }

        var adjustment = center / Count;
        adjustment.y = -lowestCoordinate;

        foreach (var metaball in this)
        {
            metaball.Position += adjustment;
        }
    }
}
