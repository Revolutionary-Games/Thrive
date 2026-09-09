using System.Collections.Generic;

/// <summary>
///   Read-only enumeration of process inputs, including environmental compounds, without allocating an enumerator.
///   The owner must not modify the backing dictionary while it is being read.
/// </summary>
public readonly struct ProcessInputAmounts(Dictionary<Compound, float> inputs)
{
    public Dictionary<Compound, float>.Enumerator GetEnumerator()
    {
        return inputs.GetEnumerator();
    }
}
