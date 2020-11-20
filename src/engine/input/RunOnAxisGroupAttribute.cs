using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[AttributeUsage(AttributeTargets.Method)]
public class RunOnAxisGroupAttribute : InputAttribute
{
    private readonly List<RunOnAxisAttribute> axes = new List<RunOnAxisAttribute>();

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
        var param = axes.Select(axis => axis.CurrentResult).Cast<object>().ToList();

        if (!InvokeWithNoInput && param.All(p => (int)p == 0))
            return;

        param.Insert(0, delta);
        CallMethod(param.ToArray());
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
