using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   An input group shown in the input tab in the options.
///   Has multiple <see cref="InputActionItem">InputActions</see>.
///   Used by OptionsMenu>Inputs>InputGroupContainer>InputGroupItem
/// </summary>
public partial class InputGroupItem : VBoxContainer
{
    [Export]
    public NodePath? InputGroupHeaderPath;

    [Export]
    public NodePath InputActionsContainerPath = null!;

#pragma warning disable CA2213
    private Label? inputGroupHeader;
    private VBoxContainer inputActionsContainer = null!;
#pragma warning restore CA2213

    private string groupName = "error";

    private FocusFlowDynamicChildrenHelper focusHelper = null!;

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

        focusHelper = new FocusFlowDynamicChildrenHelper(this,
            FocusFlowDynamicChildrenHelper.NavigationToChildrenDirection.None,
            FocusFlowDynamicChildrenHelper.NavigationInChildrenDirection.Vertical);
    }

    public InputActionItem GetLastInputInGroup()
    {
        return Actions.Last();
    }

    /// <summary>
    ///   Called when the parent has tweaked navigation layout and this object should tweak its children's layouts
    /// </summary>
    public void NotifyFocusAdjusted()
    {
        focusHelper.ReReadOwnerNeighbours();
        focusHelper.ApplyNavigationFlow(Actions, Actions.SelectFirstFocusableChild());

        // focusHelper.ApplyNavigationFlow(Actions.SelectFirstFocusableChild());

        foreach (var action in Actions)
        {
            action.NotifyFocusAdjusted();
        }
    }

    internal static InputGroupItem BuildGUI(InputGroupList associatedList, NamedInputGroup data,
        InputDataList inputData)
    {
        var result = (InputGroupItem)associatedList.InputGroupItemScene.Instantiate();

        result.AssociatedList = new WeakReference<InputGroupList>(associatedList);
        result.EnvironmentId = data.EnvironmentId.ToList();
        result.GroupName = data.GroupName;

        // Build child objects
        result.Actions = data.Actions
            .Select(x => InputActionItem.BuildGUI(result, x, inputData[x.InputName].WhereNotNull())).ToList();

        // When the result is attached to the scene tree it attaches the child objects. So it *must* be attached
        // at least one otherwise the child objects leak
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (InputGroupHeaderPath != null)
            {
                InputGroupHeaderPath.Dispose();
                InputActionsContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ApplyGroupName()
    {
        if (inputGroupHeader != null)
            inputGroupHeader.Text = GroupName;
    }
}
