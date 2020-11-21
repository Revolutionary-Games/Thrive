using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Combines multiple RunOnAxisAttributes to be able to distinguish between axes
///   Can only be applied once.
/// </summary>
/// <example>
///   [RunOnMultiAxis]
///   [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1, 1 })]
///   [RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1, 1 })]
///   public void MovePlayer(float delta, int[] inputs)
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class RunOnAxisGroupAttribute : InputAttribute
{
    /// <summary>
    ///   All the axes that are managed by this Attribute
    /// </summary>
    private readonly List<RunOnAxisAttribute> axes = new List<RunOnAxisAttribute>();

    /// <summary>
    ///   Should the method be invoked when all of it's inputs are in it's idle state
    /// </summary>
    public bool InvokeWithNoInput { get; set; }

    public override bool OnInput(InputEvent @event)
    {
        var result = false;
        axes.AsParallel().ForAll(p =>
        {
            if (p.OnInput(@event))
                result = true;
        });
        return result;
    }

    public override void OnProcess(float delta)
    {
        List<(float currentValue, float defaultValue)> param =
            axes.Select(axis => (axis.CurrentResult, axis.DefaultState)).ToList();

        if (!InvokeWithNoInput && param.All(p => Math.Abs(p.currentValue - p.defaultValue) < 0.001f))
            return;

        var values = param.Select(p => p.currentValue).ToList();
        values.Insert(0, delta);
        CallMethod(values);
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
