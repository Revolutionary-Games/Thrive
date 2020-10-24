using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class RunOnMultiAxisAttribute : RunOnInputAttribute
{
    internal readonly List<RunOnAxisAttribute> DefinitionAttributes = new List<RunOnAxisAttribute>();
    private InputMultiAxis inputKeys;

    public override IInputReceiver InputReceiver
    {
        get
        {
            return InputKeys;
        }
    }

    private InputMultiAxis InputKeys
        => inputKeys ??= new InputMultiAxis(DefinitionAttributes.Select(p => p.InputReceiver as InputAxis));
}
