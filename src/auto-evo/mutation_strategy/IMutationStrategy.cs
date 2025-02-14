namespace AutoEvo;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public interface IMutationStrategy<T>
    where T : Species
{
    [JsonIgnore]
    public bool Repeatable { get; }

    /// <summary>
    ///   Generates mutations based on this strategy
    /// </summary>
    /// <param name="baseSpecies">The species to start from</param>
    /// <param name="mp">How much MP there is to do the mutations</param>
    /// <param name="lawk">
    ///     The game LAWK status, when the game was started in LAWK mode, affects which mutations are valid
    /// </param>
    /// <param name="random">The random object</param>
    /// <param name="biomeToConsider">
    ///   Gives environment to consider the mutations to be suitable for. At the time of writing,
    ///   this only biases environmental tolerance changes.
    /// </param>
    /// <returns>
    ///   List of mutated species, null if no possible mutations are found (some strategies may return an empty list
    ///   instead in this case)
    /// </returns>
    public List<Tuple<T, float>>? MutationsOf(T baseSpecies, float mp, bool lawk, Random random,
        BiomeConditions biomeToConsider);
}
