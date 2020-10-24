using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class RunOnMultiAxisAttribute : RunOnInputAttribute
{
    public RunOnMultiAxisAttribute(string json)
    {
        var result = new List<InputAxis>();
        var jsonRes = JSON.Parse(json);
        if (jsonRes.Error != 0)
            throw new ArgumentException("Invalid json: " + jsonRes.ErrorString);

        var axisArray = (Godot.Collections.Array)jsonRes.Result;
        foreach (var axis in axisArray)
        {
            var inputs = new List<(InputBool input, int associatedValue)>();
            var subArray = (Godot.Collections.Array)axis;
            foreach (var input in subArray)
            {
                var dictionary = (Godot.Collections.Dictionary)input;
                var key = dictionary.Keys.OfType<string>().First();
                var value = (int)(float)dictionary[key];
                inputs.Add((new InputBool(key), value));
            }

            result.Add(new InputAxis(inputs));
        }

        InputKeys = new InputMultiAxis(result);
    }

    public override IInputReceiver InputReceiver
    {
        get
        {
            return InputKeys;
        }
    }

    private InputMultiAxis InputKeys { get; }
}
