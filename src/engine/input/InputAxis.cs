using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Collects multiple InputBool to make up an axis of input that handles separate release and keeps track of the
///   latest key to be pressed
/// </summary>
public class InputAxis : IInputReceiver
{
    private readonly List<AxisMember> inputs = new List<AxisMember>();

    /// <summary>
    ///   Used to track order keys are pressed down
    /// </summary>
    private int inputNumber;

    public InputAxis(IEnumerable<(InputBool input, int associatedValue)> keys)
    {
        foreach (var input in keys)
        {
            inputs.Add(new AxisMember(input.input, input.associatedValue));
        }
    }

    /// <summary>
    ///   The current value of the axis. 0 if nothing is pressed. Or the value associated with the latest pressed key
    /// </summary>
    public int CurrentValue
    {
        get
        {
            int highestFoundPressed = int.MinValue;
            int foundValue = 0;

            foreach (var input in inputs)
            {
                if (input.Input.Pressed && input.LastDown >= highestFoundPressed)
                {
                    highestFoundPressed = input.LastDown;
                    foundValue = input.Value;
                }
            }

            return foundValue;
        }
    }

    private int NextInputNumber => checked(++inputNumber);

    public bool CheckInput(InputEvent @event)
    {
        foreach (var input in inputs)
        {
            if (input.Input.CheckInput(@event))
            {
                // Was consumed

                // Just to make sure that it became pressed if the logic in the InputBool is changed
                if (input.Input.Pressed)
                {
                    try
                    {
                        input.LastDown = NextInputNumber;
                    }
                    catch (OverflowException)
                    {
                        // Reset to a lower value
                        OnInputNumberOverflow();

                        input.LastDown = 0;
                        inputNumber = 0;
                    }
                }

                return true;
            }
        }

        return false;
    }

    public void FocusLost()
    {
        foreach (var input in inputs)
            input.Input.FocusLost();
    }

    private void OnInputNumberOverflow()
    {
        foreach (var input in inputs)
            input.LastDown = -input.LastDown;
    }

    private class AxisMember
    {
        public readonly InputBool Input;
        public readonly int Value;
        public int LastDown;

        public AxisMember(InputBool input, int value)
        {
            Input = input;
            Value = value;
        }
    }
}
