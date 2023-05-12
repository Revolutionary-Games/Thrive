using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Stores Godot input actions and their associated events
/// </summary>
public class InputDataList : ICloneable
{
    public InputDataList(Dictionary<string, List<SpecifiedInputKey?>> data)
    {
        Data = data;
    }

    /// <summary>
    ///   Triggered when (possibly) changed inputs are applied to the Godot input map
    /// </summary>
    public static event EventHandler? InputsRemapped;

    /// <summary>
    ///   The key map, key is the godot action name and the list contains the keys that are used to trigger that action
    /// </summary>
    public Dictionary<string, List<SpecifiedInputKey?>> Data { get; }

    public List<SpecifiedInputKey?> this[string index] => Data[index];

    public object Clone()
    {
        var result = new Dictionary<string, List<SpecifiedInputKey?>>();
        foreach (var keyValuePair in Data)
        {
            result[keyValuePair.Key] = new List<SpecifiedInputKey?>();
            foreach (var inputEventWithModifiers in keyValuePair.Value)
            {
                result[keyValuePair.Key].Add((SpecifiedInputKey?)inputEventWithModifiers?.Clone());
            }
        }

        return new InputDataList(result);
    }

    /// <summary>
    ///   Applies the current controls (from Data) to the global InputMap.
    /// </summary>
    internal void ApplyToGodotInputMap()
    {
        foreach (var action in Data)
        {
            // Skip destroying ui actions to keep the UI usable even with bad inputs
            // This doesn't seem to happen and luckily it seems controller navigation is intact for loading settings
            // made in previous versions
            if (action.Key.StartsWith("ui_") && action.Value.Count < 1)
            {
                GD.PrintErr("Skipping clearing an UI input action");
                return;
            }

            // Clear all old input keys
            InputMap.ActionEraseEvents(action.Key);

            // Register the new input keys
            foreach (var inputEvent in action.Value)
            {
                // If the game is waiting for an input for this thing, skip trying to apply it
                if (inputEvent == null)
                    return;

                InputMap.ActionAddEvent(action.Key, inputEvent.ToInputEvent());
            }
        }

        InputsRemapped?.Invoke(this, EventArgs.Empty);
    }
}
