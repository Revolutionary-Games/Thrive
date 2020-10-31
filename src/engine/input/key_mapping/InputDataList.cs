using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Stores an godot input action and its associated events
/// </summary>
public class InputDataList : ICloneable
{
    public InputDataList(Dictionary<string, List<InputEventWithModifiers>> data)
    {
        Data = data;
    }

    public Dictionary<string, List<InputEventWithModifiers>> Data { get; }

    public List<InputEventWithModifiers> this[string index] => Data[index];

    public object Clone()
    {
        var result = new Dictionary<string, List<InputEventWithModifiers>>();
        foreach (var keyValuePair in Data)
        {
            result[keyValuePair.Key] = new List<InputEventWithModifiers>();
            foreach (var inputEventWithModifiers in keyValuePair.Value)
            {
                InputEventWithModifiers newEvent = inputEventWithModifiers switch
                {
                    InputEventKey key => new InputEventKey { Scancode = key.Scancode, },
                    InputEventMouseButton mouse => new InputEventMouseButton { ButtonIndex = mouse.ButtonIndex, },
                    _ => throw new NotSupportedException($"InputType {inputEventWithModifiers.GetType()} not supported"),
                };

                newEvent.Alt = inputEventWithModifiers.Alt;
                newEvent.Control = inputEventWithModifiers.Control;
                newEvent.Shift = inputEventWithModifiers.Shift;

                result[keyValuePair.Key].Add(newEvent);
            }
        }

        return new InputDataList(result);
    }
}
