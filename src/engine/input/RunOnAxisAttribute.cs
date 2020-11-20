using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RunOnAxisAttribute : InputAttribute
{
    private Dictionary<RunOnKeyChangeAttribute, int> inputs = new Dictionary<RunOnKeyChangeAttribute, int>();

    public RunOnAxisAttribute(string[] godotInputNames, int[] associatedValues)
    {
        if (godotInputNames.Length != associatedValues.Length)
            throw new ArgumentException("input names and associated values have to be the same length");

        for (var i = 0; i < godotInputNames.Length; i++)
        {
            inputs.Add(new RunOnKeyChangeAttribute(godotInputNames[i]), associatedValues[i]);
        }
    }

    internal int CurrentResult => (int)inputs.Where(p => p.Key.HeldDown).Select(p => p.Value).DefaultIfEmpty(0).Average();

    public override bool OnInput(InputEvent @event)
    {
        var result = false;
        foreach (var input in inputs)
        {
            if (input.Key.OnInput(@event))
                result = true;
        }

        return result;
    }

    public override void OnProcess(float delta)
    {
        var currRes = CurrentResult;
        if (currRes != 0)
            CallMethod(delta, currRes);
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
