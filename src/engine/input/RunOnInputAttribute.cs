using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RunOnInputAttribute : Attribute
{
    private InputBool inputReceiver;

    public RunOnInputAttribute(string inputString, InputType type)
    {
        InputString = inputString;
        Type = type;
    }

    ~RunOnInputAttribute()
    {
        AttributesWithMethods.RemoveAll(p => p.Item2.Equals(this));
    }

    public enum InputType
    {
        Press,
        Hold,
        Relase,
    }


    public string InputString { get; }
    public InputType Type { get; }
    public InputBool InputReceiver
    {
        get
        {
            return inputReceiver ??= Type switch
            {
                InputType.Hold => new InputHold(InputString),
                InputType.Press => new InputTrigger(InputString),
                InputType.Relase => new InputReleaseTrigger(InputString),
                _ => throw new NotSupportedException("Input type not supported")
            };
        }
    }

    internal static List<Tuple<MethodBase, RunOnInputAttribute>> AttributesWithMethods { get; } = new List<Tuple<MethodBase, RunOnInputAttribute>>();
}
