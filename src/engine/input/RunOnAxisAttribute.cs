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
    private readonly Dictionary<RunOnKeyDownAttribute, MemberData> inputs =
        new Dictionary<RunOnKeyDownAttribute, MemberData>();

    /// <summary>
    ///   Used to track order keys are pressed down
    /// </summary>
    private int inputNumber;

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
        if (CurrentResult != DefaultState || InvokeAlsoWithNoInput)
            CallMethod(delta, CurrentResult);
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
