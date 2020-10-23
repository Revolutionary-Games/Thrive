using System;
using System.Collections.Generic;
using System.Reflection;

public class RunOnKeyAttribute : RunOnInputAttribute
{
    public RunOnKeyAttribute(string inputString, InputType type)
    {
        InputString = inputString;
        Type = type;
    }


    public enum InputType
    {
        Press,
        Hold,
        Relase,
    }


    public string InputString { get; }
    public InputType Type { get; }
    public override IInputReceiver InputReceiver
    {
        get
        {
            return inputReceiver ??= Type switch
            {
                InputType.Hold => new InputBool(InputString),
                InputType.Press => new InputTrigger(InputString),
                InputType.Relase => new InputReleaseTrigger(InputString),
                _ => throw new NotSupportedException("Input type not supported"),
            };
        }
    }

    internal static List<Tuple<MethodBase, RunOnKeyAttribute>> AttributesWithMethods { get; } = new List<Tuple<MethodBase, RunOnKeyAttribute>>();
}
