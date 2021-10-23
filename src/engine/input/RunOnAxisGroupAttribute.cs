using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Combines multiple RunOnAxisAttributes to be able to distinguish between axes
///   Can only be applied once.
/// </summary>
/// <example>
///   [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
///   [RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
///   [RunOnAxisGroup]
///   public void OnMovement(float delta, float forwardBackwardMovement, float leftRightMovement)
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class RunOnAxisGroupAttribute : InputAttribute
{
    /// <summary>
    ///   All the axes that are managed by this Attribute
    /// </summary>
    private readonly List<RunOnAxisAttribute> axes = new List<RunOnAxisAttribute>();

    /// <summary>
    ///   Should the method be invoked when all of the inputs are in their idle states
    /// </summary>
    public bool InvokeAlsoWithNoInput { get; set; }

    public override bool OnInput(InputEvent @event)
    {
        var wasUsed = false;

        foreach (var axis in axes)
        {
            if (axis.OnInput(@event))
                wasUsed = true;
        }

        return wasUsed;
    }

    public override void OnProcess(float delta)
    {
        List<(float currentValue, float defaultValue)> axisValues =
            axes.Select(axis => (axis.CurrentResult, axis.DefaultState)).ToList();

        // Skip process if all axes have default values, and invoke also with no input is not set
        if (!InvokeAlsoWithNoInput && axisValues.All(p => Math.Abs(p.currentValue - p.defaultValue) < 0.001f))
            return;

        var callParameters = axisValues.Select(p => p.currentValue).ToList();
        callParameters.Insert(0, delta);

        // Casting to an object[] to match CallMethods parameter declaration
        CallMethod(callParameters.Cast<object>().ToArray());
    }

    public override void FocusLost()
    {
        axes.ForEach(p => p.FocusLost());
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj) || !(obj is RunOnAxisGroupAttribute group))
            return false;

        return axes.SequenceEqual(group.axes);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397) ^ axes.GetHashCode();
        }
    }

    internal void AddAxis(RunOnAxisAttribute axis)
    {
        axes.Add(axis);
    }
}
