using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Attribute for a method, that gets called when one of the defined axis is not in its idle state.
/// </summary>
/// <example>
///   [RunOnMultiAxis]
///   [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1, 1 })]
///   [RunOnAxis(new[] { "g_move_left", "g_move_right" },        new[] { -1, 1 })]
///   public void MovePlayer(float delta, int[] inputs)
/// </example>
public class RunOnMultiAxisAttribute : RunOnInputAttribute
{
    internal readonly List<RunOnAxisAttribute> DefinitionAttributes = new List<RunOnAxisAttribute>();
    private InputAxisGroup inputKeys;

    public override IInputReceiver InputReceiver => InputKeys;

    private InputAxisGroup InputKeys
        => inputKeys ??= new InputAxisGroup(DefinitionAttributes.Select(p => p.InputReceiver as InputAxis));
}
