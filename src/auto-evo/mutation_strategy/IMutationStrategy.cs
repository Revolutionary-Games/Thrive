namespace AutoEvo;

using System;
using System.Collections.Generic;

public interface IMutationStrategy<T>
    where T : Species
{
    public bool Repeatable { get; }

    public List<Tuple<T, float>> MutationsOf(T baseSpecies, float mp);
}
