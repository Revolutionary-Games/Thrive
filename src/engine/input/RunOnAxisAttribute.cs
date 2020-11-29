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
    ///   all associated inputs
    /// </summary>
    private readonly Dictionary<RunOnKeyChangeAttribute, float> inputs =
        new Dictionary<RunOnKeyChangeAttribute, float>();

    /// <summary>
    ///   Instantiates a new RunOnAxisAttribute.
    /// </summary>
    /// <param name="godotInputNames">All godot input names</param>
    /// <param name="associatedValues">All associated values. Length must match the godotInputNames</param>
    /// <exception cref="ArgumentException">Gets thrown when the lengths don't match</exception>
    public RunOnAxisAttribute(string[] godotInputNames, float[] associatedValues)
    {
        if (godotInputNames.Length != associatedValues.Length)
            throw new ArgumentException("input names and associated values have to be the same length");

        for (var i = 0; i < godotInputNames.Length; i++)
        {
            inputs.Add(new RunOnKeyChangeAttribute(godotInputNames[i]), associatedValues[i]);
        }

        DefaultState = associatedValues.Average();
    }

    /// <summary>
    ///   The idle state
    /// </summary>
    public float DefaultState { get; }

    /// <summary>
    ///   Should the method be invoked when all of it's inputs are in it's idle state
    /// </summary>
    public bool InvokeWithNoInput { get; set; }

    /// <summary>
    ///   Get the average of all currently fired inputs.
    /// </summary>
    internal float CurrentResult =>
        inputs.Where(p => p.Key.ReadHeldOrPrimedAndResetPrimed())
            .Select(p => p.Value)
            .DefaultIfEmpty(DefaultState)
            .Average();

    public override bool OnInput(InputEvent @event)
    {
        var result = false;
        foreach (var input in inputs.AsParallel())
        {
            if (input.Key.OnInput(@event))
                result = true;
        }

        return result;
    }

    public override void OnProcess(float delta)
    {
        var currentResult = CurrentResult;
        if (InvokeWithNoInput || currentResult != DefaultState)
            CallMethod(delta, currentResult);
    }

    public override void FocusLost()
    {
        foreach (var input in inputs.AsParallel())
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
