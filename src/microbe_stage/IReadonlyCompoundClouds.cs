using System.Collections.Generic;
using Godot;

/// <summary>
///   Read-only access to the compound clouds to allow some places safely to read data in a multithreaded way regarding
///   the clouds
/// </summary>
public interface IReadonlyCompoundClouds
{
    /// <summary>
    ///   Returns the amount of specified compound available at the given position
    /// </summary>
    /// <param name="compound">The compound to look for</param>
    /// <param name="worldPosition">Position to look at</param>
    /// <param name="fraction">
    ///   Adjusts the resulting amount by multiplying with this. Used to estimate available amounts when taking into
    ///   absorption effectiveness ratio.
    /// </param>
    /// <returns>The available amount or 0</returns>
    public float AmountAvailable(Compound compound, Vector3 worldPosition, float fraction);

    /// <summary>
    ///   Returns the total amount of all compounds at position
    /// </summary>
    public void GetAllAvailableAt(Vector3 worldPosition, Dictionary<Compound, float> result,
        bool onlyAbsorbable = true);

    /// <summary>
    ///   Tries to find specified compound as close to the point as possible.
    /// </summary>
    /// <param name="position">Position to search around</param>
    /// <param name="compound">What compound to search for</param>
    /// <param name="searchRadius">How wide to search around the point</param>
    /// <param name="minConcentration">Limits search to only find concentrations higher than this</param>
    /// <returns>The nearest found point for the compound or null</returns>
    public Vector3? FindCompoundNearPoint(Vector3 position, Compound compound, float searchRadius = 200,
        float minConcentration = 120);
}
