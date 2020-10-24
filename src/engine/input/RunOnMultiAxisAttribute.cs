using System.Collections.Generic;
using System.Linq;

public class RunOnMultiAxisAttribute : RunOnInputAttribute
{
    internal readonly List<RunOnAxisAttribute> DefinitionAttributes = new List<RunOnAxisAttribute>();
    private InputMultiAxis inputKeys;

    public override IInputReceiver InputReceiver => InputKeys;

    private InputMultiAxis InputKeys
        => inputKeys ??= new InputMultiAxis(DefinitionAttributes.Select(p => p.InputReceiver as InputAxis));
}
