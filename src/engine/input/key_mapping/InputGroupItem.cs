using System;
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
    public NodePath InputGroupHeaderPath = null!;

    [Export]
    public NodePath InputActionsContainerPath = null!;

    private Label? inputGroupHeader;
    private VBoxContainer inputActionsContainer = null!;
    private string groupName = "error";

    /// <summary>
    ///   The display name for the group
    /// </summary>
    /// <example>
    ///   Movement
    /// </example>
    public string GroupName
    {
        get => groupName;
        set
        {
            if (groupName == value)
                return;

            groupName = value;
            ApplyGroupName();
        }
    }

    /// <summary>
    ///   The associated actions the group contains
    /// </summary>
    public List<InputActionItem> Actions { get; private set; } = null!;

    /// <summary>
    ///   A list of environments the actions of the group are associated with.
    ///   Used by the key conflict detection.
    /// </summary>
    public List<string> EnvironmentId { get; private set; } = null!;

    /// <summary>
    ///   The top level input list this input group is associated with
    /// </summary>
    internal WeakReference<InputGroupList> AssociatedList { get; private set; } = null!;

    public override void _Ready()
    {
        if (Actions == null)
            throw new InvalidOperationException($"{nameof(Actions)} can't be null");

        if (EnvironmentId == null)
            throw new InvalidOperationException($"{nameof(EnvironmentId)} can't be null");

        if (AssociatedList == null)
            throw new InvalidOperationException($"{nameof(AssociatedList)} can't be null");

        inputGroupHeader = GetNode<Label>(InputGroupHeaderPath);
        inputActionsContainer = GetNode<VBoxContainer>(InputActionsContainerPath);

        ApplyGroupName();

        // Add the actions to the godot scene tree
        foreach (var action in Actions)
        {
            inputActionsContainer.AddChild(action);
        }
    }

    internal static InputGroupItem BuildGUI(InputGroupList associatedList, NamedInputGroup data,
        InputDataList inputData)
    {
        var result = (InputGroupItem)associatedList.InputGroupItemScene.Instance();

        result.AssociatedList = new WeakReference<InputGroupList>(associatedList);
        result.EnvironmentId = data.EnvironmentId.ToList();
        result.GroupName = data.GroupName;

        // Build child objects
        result.Actions = data.Actions
            .Select(x => InputActionItem.BuildGUI(result, x, inputData[x.InputName].DiscardNulls())).ToList();

        // When the result is attached to the scene tree it attaches the child objects. So it *must* be attached
        // at least one otherwise the child objects leak
        return result;
    }

    private void ApplyGroupName()
    {
        if (inputGroupHeader != null)
            inputGroupHeader.Text = GroupName;
    }
}
