using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

public class RunOnAxisAttribute : RunOnInputAttribute
{
    public RunOnAxisAttribute(string[] inputs, int[] associatedValues)
    {
        if (inputs.Length != associatedValues.Length)
            throw new TargetParameterCountException("inputs and associatedValues need to be the same length");
        InputKeys = new List<(InputBool input, int associatedValue)>();
        for (var i = 0; i < inputs.Length; i++)
        {
            InputKeys.Add((new InputBool(inputs[i]), associatedValues[i]));
        }
    }

    public List<(InputBool input, int associatedValue)> InputKeys { get; }

    public override IInputReceiver InputReceiver
    {
        get
        {
            return inputReceiver ??= new InputAxis(InputKeys);
        }
    }

}
