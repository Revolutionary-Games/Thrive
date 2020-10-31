using System;
using System.Collections.Generic;

/// <summary>
///   Stores an godot input action and its associated events
/// </summary>
public class InputDataList : ICloneable
{
    public InputDataList(Dictionary<string, List<ThriveInputEventWithModifiers>> data)
    {
        Data = data;
    }

    public Dictionary<string, List<ThriveInputEventWithModifiers>> Data { get; }

    public List<ThriveInputEventWithModifiers> this[string index] => Data[index];

    public object Clone()
    {
        var result = new Dictionary<string, List<ThriveInputEventWithModifiers>>();
        foreach (var keyValuePair in Data)
        {
            result[keyValuePair.Key] = new List<ThriveInputEventWithModifiers>();
            foreach (var inputEventWithModifiers in keyValuePair.Value)
            {
                result[keyValuePair.Key].Add((ThriveInputEventWithModifiers)inputEventWithModifiers.Clone());
            }
        }

        return new InputDataList(result);
    }
}
