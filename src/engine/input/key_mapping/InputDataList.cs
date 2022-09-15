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
        var preservedControllerActions = new List<InputEvent>();

        foreach (var action in Data)
        {
            // TODO: add support for controller input binding changing, for now leave those as is
            foreach (InputEvent actionListItem in InputMap.GetActionList(action.Key))
            {
                if (actionListItem is InputEventJoypadButton or InputEventJoypadMotion or InputEventGesture)
                    preservedControllerActions.Add(actionListItem);
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

            foreach (var preservedAction in preservedControllerActions)
            {
                InputMap.ActionAddEvent(action.Key, preservedAction);
            }

            preservedControllerActions.Clear();
        }

        InputsRemapped?.Invoke(this, EventArgs.Empty);
    }
}
