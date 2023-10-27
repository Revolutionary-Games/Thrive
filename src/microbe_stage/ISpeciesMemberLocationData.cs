using System;
using System.Collections.Generic;
using DefaultEcs;
using Godot;

/// <summary>
///   Provides fast access to microbe species member location information
/// </summary>
/// <remarks>
///   <para>
///     TODO: should probably make a dedicated system for this rather than keeping the functionality in the microbe AI
///     and having external places in the code call it
///   </para>
/// </remarks>
public interface ISpeciesMemberLocationData
{
    /// <summary>
    ///   Returns a list of all known members of the given species if any exist
    /// </summary>
    /// <param name="species">The species to look for</param>
    /// <returns>List with members of this species along with their positions and sizes</returns>
    public IReadOnlyList<(Entity Entity, Vector3 Position, float EngulfSize)>? GetSpeciesMembers(Species species);
}

public static class SpeciesMemberLocationDataHelpers
{
    /// <summary>
    ///   Tries to find specified Species as close to the point as possible.
    /// </summary>
    /// <param name="locationData">Access to data about microbe positions</param>
    /// <param name="position">Position to search around</param>
    /// <param name="species">What species to search for</param>
    /// <param name="searchRadius">How wide to search around the point</param>
    /// <param name="foundMicrobe">
    ///   When this returns true then this contains the entity that is the closest species member to the given position
    /// </param>
    /// <param name="foundPosition">The position of <see cref="foundMicrobe"/> when that value is valid</param>
    /// <returns>True if a nearest species member is found</returns>
    public static bool FindSpeciesNearPoint(this ISpeciesMemberLocationData locationData, Vector3 position,
        Species species, float searchRadius, out Entity foundMicrobe, out Vector3 foundPosition)
    {
        if (searchRadius < 1)
            throw new ArgumentException("searchRadius must be >= 1");

        bool closestFound = false;
        float nearestDistanceSquared = float.MaxValue;

        var searchRadiusSquared = searchRadius * searchRadius;

        var members = locationData.GetSpeciesMembers(species);

        // These are set here to make the compiler allow us to simply exit this method as the exit condition is complex
        foundMicrobe = default;
        foundPosition = Vector3.Zero;

        if (members == null)
        {
            return false;
        }

        foreach (var microbe in members)
        {
            var microbeGlobalPosition = microbe.Position;

            // Skip candidates for performance
            if (Math.Abs(microbeGlobalPosition.x - position.x) > searchRadius ||
                Math.Abs(microbeGlobalPosition.y - position.y) > searchRadius)
            {
                continue;
            }

            var distanceSquared = (microbeGlobalPosition - position).LengthSquared();

            if (distanceSquared < nearestDistanceSquared &&
                distanceSquared < searchRadiusSquared &&
                distanceSquared > 1)
            {
                closestFound = true;
                nearestDistanceSquared = distanceSquared;
                foundMicrobe = microbe.Entity;
                foundPosition = microbeGlobalPosition;
            }
        }

        return closestFound;
    }
}
