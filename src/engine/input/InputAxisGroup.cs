using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Collects multiple InputAxis to make up multiple of them
/// </summary>
public class InputAxisGroup : IInputReceiver
{
    private readonly List<InputAxis> inputs;

    public InputAxisGroup(IEnumerable<InputAxis> keys)
    {
        inputs = keys.ToList();
    }

    /// <summary>
    ///   The current values of the axis.
    /// </summary>
    private int[] CurrentValues
    {
        get
        {
            return inputs.Select(p => p.CurrentValue).ToArray();
        }
    }

    public bool CheckInput(InputEvent @event)
    {
        var returnValue = false;

        foreach (var input in inputs)
        {
            if (input.CheckInput(@event))
                returnValue = true;
        }

        return returnValue;
    }

    public object GetValueForCallback()
    {
        return CurrentValues;
    }

    public bool ShouldTriggerCallbacks()
    {
        return true;
    }

    public void FocusLost()
    {
        foreach (var input in inputs)
            input.FocusLost();
    }
}
