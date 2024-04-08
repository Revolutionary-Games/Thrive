using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Stores Godot input actions and their associated events
/// </summary>
public class InputDataList : ICloneable
{
    public InputDataList(Dictionary<string, List<SpecifiedInputKey>> data)
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
    ///   Applies the current controls (from Data) to the global InputMap.
    /// </summary>
    internal void ApplyToGodotInputMap()
    {
        bool printedUIWarning = false;

        foreach (var action in Data)
        {
            // Skip destroying ui actions to keep the UI usable even with bad inputs
            // This doesn't seem to happen and luckily it seems controller navigation is intact for loading settings
            // made in previous versions
            // TODO: this might need changes when ui keys are fully rebindable
            if (action.Key.StartsWith("ui_") && action.Value.Count < 1)
            {
                if (!printedUIWarning)
                {
                    GD.PrintErr("Skipping clearing an UI input action: ", action.Key);
                    printedUIWarning = true;
                }

                continue;
            }

            var keyName = new StringName(action.Key);

            // Clear all old input keys
            InputMap.ActionEraseEvents(keyName);

            // Register the new input keys
            foreach (var inputEvent in action.Value)
            {
                // It used to be the case that input event could be null here for pending inputs, that is no longer the
                // case (instead they are missing from the list)

                InputMap.ActionAddEvent(keyName, inputEvent.ToInputEvent());
            }
        }

        InputsRemapped?.Invoke(this, EventArgs.Empty);
    }
}
