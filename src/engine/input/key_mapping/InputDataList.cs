using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Stores godot input actions and their associated events
/// </summary>
public class InputDataList : ICloneable
{
    public InputDataList(Dictionary<string, List<SpecifiedInputKey>> data)
    {
        Data = data;
    }

    public Dictionary<string, List<SpecifiedInputKey>> Data { get; }

    public List<SpecifiedInputKey> this[string index] => Data[index];

    public object Clone()
    {
        var result = new Dictionary<string, List<SpecifiedInputKey>>();
        foreach (var keyValuePair in Data)
        {
            result[keyValuePair.Key] = new List<SpecifiedInputKey>();
            foreach (var inputEventWithModifiers in keyValuePair.Value)
            {
                result[keyValuePair.Key].Add((SpecifiedInputKey)inputEventWithModifiers.Clone());
            }
        }

        return new InputDataList(result);
    }

    /// <summary>
    ///   Applies the current controls to the InputMap.
    /// </summary>
    internal void ApplyToGodotInputMap()
    {
        foreach (var action in Data)
        {
            // Clear all old input events
            InputMap.ActionEraseEvents(action.Key);

            // Add the new input events
            foreach (var inputEvent in action.Value)
            {
                // If the game is waiting for an input
                if (inputEvent == null)
                    return;

                InputMap.ActionAddEvent(action.Key, inputEvent.ToInputEvent());
            }
        }
    }
}
