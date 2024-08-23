namespace AutoEvo;

using System;
using System.Collections.Generic;

public interface IMutationStrategy<T>
    where T : Species
{
    public bool Repeatable { get; }

    /// <summary>
    ///   Generates mutations based on this strategy
    /// </summary>
    /// <param name="baseSpecies">The species to start from</param>
    /// <param name="mp">How much MP there is to do the mutations</param>
    /// <param name="lawk">
    ///   The game LAWK status, should be true when game was started in LAWK mode, affects which mutations are valid
    /// </param>
    /// <returns>
    ///   List of mutated species, null if no possible mutations are found (some strategies may return an empty list
    ///   instead in this case)
    /// </returns>
    public List<Tuple<T, float>>? MutationsOf(T baseSpecies, float mp, bool lawk);
}
