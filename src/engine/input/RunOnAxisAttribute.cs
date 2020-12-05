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
    private readonly Dictionary<RunOnKeyDownAttribute, float> inputs =
        new Dictionary<RunOnKeyDownAttribute, float>();

    private RunOnKeyDownAttribute currentlyPressed;

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
            inputs.Add(new RunOnKeyDownAttribute(inputNames[i]), associatedValues[i]);
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
    public bool InvokeWithNoInput { get; set; }

    /// <summary>
    ///   Get the average of all currently fired inputs.
    /// </summary>
    public float CurrentResult
    {
        get
        {
            if (currentlyPressed == null)
                return DefaultState;

            if (!currentlyPressed.ReadHeldOrPrimedAndResetPrimed())
            {
                currentlyPressed = inputs.Keys.FirstOrDefault(p => p.ReadHeldOrPrimedAndResetPrimed());
                return currentlyPressed == null ? DefaultState : inputs[currentlyPressed];
            }

            return inputs[currentlyPressed];
        }
    }

    public override bool OnInput(InputEvent @event)
    {
        var wasUsed = false;

        foreach (var input in inputs)
        {
            if (input.Key.OnInput(@event))
            {
                wasUsed = true;
                currentlyPressed = input.Key;
            }
        }

        return wasUsed;
    }

    public override void OnProcess(float delta)
    {
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
}
