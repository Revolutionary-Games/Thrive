﻿using Godot;

public class MacroscopicMetaballLayout : MetaballLayout<MacroscopicMetaball>
{
    /// <summary>
    ///   Repositions the bottom-most metaballs to touch the ground, and the center of all metaballs to be aligned
    ///   above 0,0
    /// </summary>
    public bool RepositionToGround()
    {
        float lowestCoordinate = 0;
        var center = Vector3.Zero;

        foreach (var metaball in this)
        {
            center += metaball.Position;

            // Make the bottom edge of the lowest metaball touch the "ground"
            var bottom = metaball.Position.Y - metaball.Radius;

            if (bottom < lowestCoordinate)
                lowestCoordinate = bottom;
        }

        var adjustment = center / Count;
        adjustment.Y = lowestCoordinate;

        // Should this allow slight movement?
        if (adjustment.X == 0 && adjustment.Y == 0 && adjustment.Z == 0)
            return false;

        foreach (var metaball in this)
        {
            metaball.Position -= adjustment;
        }

        return true;
    }
}
