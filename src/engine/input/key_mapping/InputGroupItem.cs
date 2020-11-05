using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   An input group shown in the input tab in the options.
///   Has multiple <see cref="InputActionItem">InputActions</see>.
///   Used by OptionsMenu>Inputs>InputGroupContainer>InputGroupItem
/// </summary>
public class InputGroupItem : VBoxContainer
{
    [Export]
    public NodePath InputGroupHeaderPath;

    [Export]
    public NodePath InputActionsContainerPath;

    private Label inputGroupHeader;
    private VBoxContainer inputActionsContainer;

    /// <summary>
    ///   The display name for the group
    /// </summary>
    /// <example>
    ///   Movement
    /// </example>
    public string GroupName { get; set; }

    /// <summary>
    ///   The associated actions the group contains
    /// </summary>
    public List<InputActionItem> Actions { get; set; }

    /// <summary>
    ///   A list of environments the actions of the group are associated with.
    ///   Used by the conflict detection.
    /// </summary>
    public List<string> EnvironmentId { get; set; }

    public override void _Ready()
    {
        inputGroupHeader = GetNode<Label>(InputGroupHeaderPath);
        inputActionsContainer = GetNode<VBoxContainer>(InputActionsContainerPath);

        inputGroupHeader.Text = GroupName;

        // Add the actions to the godot element tree
        foreach (var action in Actions)
        {
            inputActionsContainer.AddChild(action);
        }
    }

    internal static InputGroupItem BuildGUI(InputGroupList caller, NamedInputGroup data, InputDataList inputData)
    {
        var result = (InputGroupItem)InputGroupList.InputGroupItemScene.Instance();

        result.EnvironmentId = data.EnvironmentId.ToList();
        result.GroupName = data.GroupName;
        result.Actions = data.Actions.Select(x => InputActionItem.BuildGUI(result, x, inputData[x.InputName])).ToList();
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        inputGroupHeader?.Dispose();
        inputActionsContainer?.Dispose();

        base.Dispose(disposing);

        foreach (var inputActionItem in Actions)
        {
            inputActionItem.Dispose();
        }
    }
}
