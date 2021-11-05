using System;
using Godot;

/// <summary>
///   Defines one specific event associated with an <see cref="InputActionItem">InputActionItem</see>.
///   Handles the input rebinding and the asking the user for a new input action.
/// </summary>
/// <remarks>
///   <para>
///     Used by OptionsMenu>Inputs>InputGroupContainer>InputGroupItem>InputActionItem>InputEventItem
///   </para>
/// </remarks>
public class InputEventItem : Node
{
    [Export]
    public NodePath ButtonPath;

    [Export]
    public NodePath XButtonPath;

    private Button button;
    private Button xButton;
    private bool wasPressingButton;

    /// <summary>
    ///   If this is currently awaiting the user to press a button (for rebinding purposes)
    /// </summary>
    public bool WaitingForInput { get; private set; }

    /// <summary>
    ///   The currently assigned key binding for this event.
    ///   null if this InputEventItem was just created
    /// </summary>
    public SpecifiedInputKey AssociatedEvent { get; private set; }

    /// <summary>
    ///   The game action InputEventItem is associated with
    /// </summary>
    internal WeakReference<InputActionItem> AssociatedAction { get; set; }

    /// <summary>
    ///   If this key binding was just created and has never been assigned with a value, so immediately prompt the user
    ///   for a value
    /// </summary>
    internal bool JustAdded { get; set; }

    private InputActionItem Action
    {
        get
        {
            if (!AssociatedAction.TryGetTarget(out var associatedAction))
                return null;

            return associatedAction;
        }
    }

    private InputGroupItem Group
    {
        get
        {
            var action = Action;

            InputGroupItem associatedGroup = null;
            action?.AssociatedGroup.TryGetTarget(out associatedGroup);

            return associatedGroup;
        }
    }

    private InputGroupList GroupList
    {
        get
        {
            var group = Group;

            InputGroupList associatedList = null;
            group?.AssociatedList.TryGetTarget(out associatedList);

            return associatedList;
        }
    }

    public override void _Ready()
    {
        button = GetNode<Button>(ButtonPath);
        xButton = GetNode<Button>(XButtonPath);

        if (JustAdded)
        {
            OnRebindButtonPressed();
        }
        else
        {
            UpdateButtonText();
        }

        // ESC must not be re-assignable or removable, otherwise it can't be added back because ESC is the only key
        // reserved this way to serve as the way to cancel a rebind action.
        if (AssociatedEvent?.Code == (uint)KeyList.Escape)
        {
            button.Disabled = true;
            xButton.Disabled = true;
        }
    }

    /// <summary>
    ///   Delete this event from the associated action and update the godot InputMap
    /// </summary>
    public void Delete()
    {
        Action.Inputs.Remove(this);
        GroupList.ControlsChanged();
    }

    /// <summary>
    ///   Performs the key reassigning.
    ///   Checks if it is waiting for a user input and if there are any conflicts (opens a warning dialog
    ///   if there is any).
    ///   Overrides the old input with the new one and update the godot InputMap.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        var groupList = GroupList;

        if (groupList == null)
        {
            GD.PrintErr("InputEventItem has no group list");
            return;
        }

        if (groupList.IsConflictDialogOpen())
            return;

        // Only InputEventMouseButton and InputEventKey are supported now
        if (!(@event is InputEventMouseButton) && !(@event is InputEventKey))
            return;

        // Hacky custom button press detection
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (xButton.IsHovered() && !xButton.Disabled)
            {
                GetTree().SetInputAsHandled();

                Delete();

                // Rebind canceled, alert the InputManager so it can resume getting input
                InputManager.PerformingRebind = false;

                return;
            }

            if (button.IsHovered() && !WaitingForInput && mouseEvent.Pressed && !button.Disabled)
            {
                GetTree().SetInputAsHandled();
                OnButtonPressed(mouseEvent);
                return;
            }
        }

        if (!WaitingForInput)
            return;

        if (wasPressingButton)
        {
            wasPressingButton = false;
            return;
        }

        if (@event is InputEventKey key)
        {
            switch (key.Scancode)
            {
                case (uint)KeyList.Escape:
                {
                    GetTree().SetInputAsHandled();

                    InputGroupList.WasListeningForInput = true;
                    WaitingForInput = false;

                    // Rebind canceled, alert the InputManager so it can resume getting input
                    InputManager.PerformingRebind = false;

                    if (AssociatedEvent == null)
                    {
                        Delete();
                    }
                    else
                    {
                        UpdateButtonText();
                    }

                    return;
                }

                case (uint)KeyList.Alt:
                case (uint)KeyList.Shift:
                case (uint)KeyList.Control:
                    return;
            }
        }
        else if (@event is InputEventMouseButton mouse)
        {
            if (!mouse.Pressed)
                return;
        }

        // The old key input event. Null if this event is assigned a value the first time.
        var old = AssociatedEvent;
        AssociatedEvent = new SpecifiedInputKey((InputEventWithModifiers)@event);

        GetTree().SetInputAsHandled();

        // Check conflicts, and don't proceed if there is a conflict
        if (CheckNewKeyConflicts(@event, groupList, old))
            return;

        OnKeybindingSuccessfullyChanged();
    }

    public override bool Equals(object obj)
    {
        if (!(obj is InputEventItem input))
            return false;

        return Equals(AssociatedEvent, input.AssociatedEvent);
    }

    public override int GetHashCode()
    {
        return AssociatedEvent.GetHashCode();
    }

    internal static InputEventItem BuildGUI(InputActionItem associatedAction, SpecifiedInputKey @event)
    {
        var result = (InputEventItem)associatedAction.GroupList.InputEventItemScene.Instance();

        result.AssociatedAction = new WeakReference<InputActionItem>(associatedAction);
        result.AssociatedEvent = @event;
        return result;
    }

    private bool CheckNewKeyConflicts(InputEvent @event, InputGroupList groupList, SpecifiedInputKey old)
    {
        // Get the conflicts with the new input.
        var conflict = groupList.Conflicts(this);
        if (conflict != null)
        {
            AssociatedEvent = old;

            // If there are conflicts detected reset the changes and ask the user.
            groupList.ShowInputConflictDialog(this, conflict, (InputEventWithModifiers)@event);
            return true;
        }

        var associatedAction = Action;

        // Check if the input is already defined for this action
        // This code works by finding a pair
        for (var i = 0; i < associatedAction.Inputs.Count; i++)
        {
            for (var x = i + 1; x < associatedAction.Inputs.Count; x++)
            {
                // Pair found (input already defined)
                if (!associatedAction.Inputs[i].Equals(associatedAction.Inputs[x]))
                    continue;

                // Set AssociatedEvent to null to not delete the wrong InputEventItem,
                // because Equals treats it the same with the same AssociatedEvent.
                AssociatedEvent = null;
                Delete();
                return true;
            }
        }

        return false;
    }

    private void OnKeybindingSuccessfullyChanged()
    {
        WaitingForInput = false;
        JustAdded = false;
        InputGroupList.WasListeningForInput = false;

        // Update the godot InputMap
        GroupList?.ControlsChanged();

        // Update the button text
        UpdateButtonText();

        // Rebind succeeded, alert the InputManager so it can resume getting input
        InputManager.PerformingRebind = false;
    }

    /// <summary>
    ///   Detect mouse button presses on this input key Control
    /// </summary>
    private void OnButtonPressed(InputEventMouseButton @event)
    {
        var groupList = GroupList;

        if (groupList.IsConflictDialogOpen())
            return;

        if (groupList.ListeningForInput)
            return;

        switch (@event.ButtonIndex)
        {
            case (int)ButtonList.Left:
                wasPressingButton = true;
                OnRebindButtonPressed();
                break;
            case (int)ButtonList.Right:
                Delete();
                break;
        }
    }

    private void OnRebindButtonPressed()
    {
        WaitingForInput = true;
        button.Text = TranslationServer.Translate("PRESS_KEY_DOT_DOT_DOT");
        xButton.Visible = true;

        // Notify InputManager that input rebinding has started and it should not react to input
        InputManager.PerformingRebind = true;
    }

    private void UpdateButtonText()
    {
        button.Text = AssociatedEvent.ToString();
        xButton.Visible = false;
    }
}
