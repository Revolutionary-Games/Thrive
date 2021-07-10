using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined axis is not in its idle state.
///   Can be applied multiple times. [RunOnAxisGroup] required to distinguish between the axes.
/// </summary>
/// <example>
///   [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f })]
///   public void Zoom(float delta, float value)
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RunOnAxisAttribute : InputAttribute
{
    /// <summary>
    ///   All associated inputs for this axis
    /// </summary>
    private Dictionary<RunOnKeyAttribute, MemberData> inputs = new Dictionary<RunOnKeyAttribute, MemberData>();

    /// <summary>
    ///   Used to track order keys are pressed down
    /// </summary>
    private int inputNumber;

    private bool useDiscreteKeyInputs;

    /// <summary>
    ///   Instantiates a new RunOnAxisAttribute.
    /// </summary>
    /// <param name="inputNames">All godot input names</param>
    /// <param name="associatedValues">All associated values. Length must match the inputNames</param>
    /// <exception cref="ArgumentException">Gets thrown when the lengths don't match</exception>
    public RunOnAxisAttribute(string[] inputNames, float[] associatedValues)
    {
        if (inputNames.Length != associatedValues.Length)
            throw new ArgumentException("input names and associated values have to be the same length");

        for (var i = 0; i < inputNames.Length; i++)
        {
            inputs.Add(new RunOnKeyDownAttribute(inputNames[i]), new MemberData(associatedValues[i]));
        }

        // Round to make sure that there isn't a really close number instead of the exactly wanted default value
        DefaultState = (float)Math.Round(associatedValues.Average(), 4);
    }

    /// <summary>
    ///   The idle state. This is the average value of all the associated input values on this axis
    /// </summary>
    public float DefaultState { get; }

    /// <summary>
    ///   Should the method be invoked when all of this object's inputs are in their idle states
    /// </summary>
    public bool InvokeAlsoWithNoInput { get; set; }

    /// <summary>
    ///   If true then the axis members only trigger on key down and repeat. Should not be changed after initialization
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: would be nice to not have to recreate the objects here
    ///   </para>
    /// </remarks>
    public bool UseDiscreteKeyInputs
    {
        get => useDiscreteKeyInputs;
        set
        {
            if (useDiscreteKeyInputs == value)
                return;

            useDiscreteKeyInputs = value;

            // Change the objects in inputs used for the key handling to the right type
            var newInputs = new Dictionary<RunOnKeyAttribute, MemberData>();

            foreach (var entry in inputs)
            {
                if (useDiscreteKeyInputs)
                {
                    newInputs.Add(new RunOnKeyDownWithRepeatAttribute(entry.Key.InputName), entry.Value);
                }
                else
                {
                    newInputs.Add(new RunOnKeyDownAttribute(entry.Key.InputName), entry.Value);
                }
            }

            inputs = newInputs;
        }
    }

    /// <summary>
    ///   Get the currently active axis member value, or DefaultState
    /// </summary>
    public float CurrentResult
    {
        get
        {
            int highestFoundPressed = int.MinValue;
            float foundValue = DefaultState;

            foreach (var entry in inputs)
            {
                if (entry.Key.ReadHeldOrPrimedAndResetPrimed() && entry.Value.LastDown >= highestFoundPressed)
                {
                    highestFoundPressed = entry.Value.LastDown;
                    foundValue = entry.Value.Value;
                }
            }

            return foundValue;
        }
    }

    private int NextInputNumber => checked(++inputNumber);

    public override bool OnInput(InputEvent @event)
    {
        var wasUsed = false;

        foreach (var input in inputs)
        {
            if (input.Key.OnInput(@event) && input.Key.HeldDown)
            {
                wasUsed = true;

                try
                {
                    input.Value.LastDown = NextInputNumber;
                }
                catch (OverflowException)
                {
                    // Reset to a lower value
                    OnInputNumberOverflow();

                    input.Value.LastDown = 0;
                    inputNumber = 0;
                }
            }
        }

        return wasUsed;
    }

    public override void OnProcess(float delta)
    {
        // If UseDiscreteKeyInputs is true CurrentResult evaluation actually changes state, which is not optimal...
        var currentResult = CurrentResult;
        if (currentResult != DefaultState || InvokeAlsoWithNoInput)
            CallMethod(delta, currentResult);
    }

    public override void FocusLost()
    {
        foreach (var input in inputs)
        {
            input.Key.FocusLost();
        }
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj) || !(obj is RunOnAxisAttribute axis))
            return false;

        return inputs.Count == axis.inputs.Count && !inputs.Except(axis.inputs).Any();
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ inputs.GetHashCode();
        }
    }

    private void OnInputNumberOverflow()
    {
        foreach (var input in inputs)
            input.Value.LastDown = -input.Value.LastDown;
    }

    private class MemberData
    {
        public readonly float Value;
        public int LastDown;

        public MemberData(float value)
        {
            Value = value;
        }
    }
}
