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
public class InputEventItem : MarginContainer
{
    [Export]
    public NodePath? ButtonPath;

    [Export]
    public NodePath XButtonPath = null!;

#pragma warning disable CA2213
    private Button button = null!;
    private Button xButton = null!;
    private bool wasPressingButton;

    private Control? alternativeButtonContentToText;
#pragma warning restore CA2213

    /// <summary>
    ///   If this is currently awaiting the user to press a button (for rebinding purposes)
    /// </summary>
    public bool WaitingForInput { get; private set; }

    /// <summary>
    ///   The currently assigned key binding for this event. null if this InputEventItem was just created.
    /// </summary>
    public SpecifiedInputKey? AssociatedEvent { get; private set; }

    /// <summary>
    ///   The game action InputEventItem is associated with
    /// </summary>
    internal WeakReference<InputActionItem>? AssociatedAction { get; set; }

    /// <summary>
    ///   If this key binding was just created and has never been assigned with a value, so immediately prompt the user
    ///   for a value
    /// </summary>
    internal bool JustAdded { get; set; }

    private InputActionItem? Action
    {
        get
        {
            if (AssociatedAction == null)
                return null;

            if (!AssociatedAction.TryGetTarget(out var associatedAction))
                return null;

            return associatedAction;
        }
    }

    private InputGroupItem? Group
    {
        get
        {
            var action = Action;

            InputGroupItem? associatedGroup = null;
            action?.AssociatedGroup?.TryGetTarget(out associatedGroup);

            return associatedGroup;
        }
    }

    private InputGroupList? GroupList
    {
        get
        {
            var group = Group;

            InputGroupList? associatedList = null;
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

    public override void _EnterTree()
    {
        base._EnterTree();

        KeyPromptHelper.IconsChanged += OnIconsChanged;

        // We need to also listen for this as when controller type changes even when not using controller input,
        // we want to know about that
        KeyPromptHelper.ControllerTypeChanged += OnControllerChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        KeyPromptHelper.IconsChanged -= OnIconsChanged;
        KeyPromptHelper.ControllerTypeChanged -= OnControllerChanged;
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

        // Ignore some unbindable inputs event types
        if (@event is InputEventMIDI or InputEventScreenDrag or InputEventScreenTouch or InputEventGesture
            or InputEventMouseMotion)
        {
            return;
        }

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
        else if (!WaitingForInput && @event is InputEventJoypadButton joypadButton && button.HasFocus())
        {
            if (joypadButton.IsActionPressed("ui_select"))
            {
                // TODO: show somewhere in the GUI that this is for unbinding inputs

                GetTree().SetInputAsHandled();
                OnButtonPressed(new InputEventMouseButton
                {
                    ButtonIndex = (int)ButtonList.Right,
                });
            }
            else if (joypadButton.IsActionPressed("ui_accept"))
            {
                GetTree().SetInputAsHandled();
                OnButtonPressed(new InputEventMouseButton
                {
                    ButtonIndex = (int)ButtonList.Left,
                });
            }
        }

        if (!WaitingForInput)
            return;

        if (wasPressingButton)
        {
            wasPressingButton = false;
            return;
        }

        // We ignore a bunch of events that are not pressed events, additionally we have special handing for escape
        // and the modifier keys
        if (@event is InputEventKey key)
        {
            if (!key.Pressed)
                return;

            switch (key.Scancode)
            {
                case (uint)KeyList.Escape:
                {
                    GetTree().SetInputAsHandled();

                    WaitingForInput = false;
                    if (alternativeButtonContentToText != null)
                        alternativeButtonContentToText.Visible = true;

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

                // TODO: allow binding these (probably need to wait a bit to see if a second keypress is coming soon)
                // See: https://github.com/Revolutionary-Games/Thrive/issues/3887
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
        else if (@event is InputEventJoypadButton controllerButton)
        {
            if (!controllerButton.Pressed)
                return;

            // TODO: controller device keybinding mode setting
            controllerButton.Device = -1;
        }
        else if (@event is InputEventJoypadMotion controllerAxis)
        {
            // Ignore too low values to disallow this accidentally happening
            if (Math.Abs(controllerAxis.AxisValue) < Constants.CONTROLLER_AXIS_REBIND_REQUIRED_STRENGTH)
                return;

            // TODO: should we allow binding L2 and R2 as axis inputs
            if (controllerAxis.Axis is (int)JoystickList.R2 or (int)JoystickList.L2)
                return;

            // TODO: controller device keybinding mode setting
            controllerAxis.Device = -1;
        }

        // The old key input event. Null if this event is assigned a value the first time.
        var old = AssociatedEvent;

        try
        {
            AssociatedEvent = new SpecifiedInputKey(@event);
        }
        catch (Exception e)
        {
            GD.PrintErr("Unbindable input got too far, error: ", e);
            return;
        }

        GetTree().SetInputAsHandled();

        // Check conflicts, and don't proceed if there is a conflict
        if (CheckNewKeyConflicts(@event, groupList, old))
            return;

        OnKeybindingSuccessfullyChanged();
    }

    /// <summary>
    ///   Delete this event from the associated action and update the godot InputMap
    /// </summary>
    public void Delete()
    {
        Action?.Inputs.Remove(this);
        GroupList?.ControlsChanged();
    }

    public override bool Equals(object? obj)
    {
        if (!(obj is InputEventItem input))
            return false;

        return Equals(AssociatedEvent, input.AssociatedEvent);
    }

    public override int GetHashCode()
    {
        return AssociatedEvent?.GetHashCode() ?? 13;
    }

    internal static InputEventItem BuildGUI(InputActionItem associatedAction, SpecifiedInputKey @event)
    {
        if (associatedAction.GroupList == null)
            throw new ArgumentException("Action doesn't have group list", nameof(associatedAction));

        var result = (InputEventItem)associatedAction.GroupList.InputEventItemScene.Instance();

        result.AssociatedAction = new WeakReference<InputActionItem>(associatedAction);
        result.AssociatedEvent = @event;
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ButtonPath != null)
            {
                ButtonPath.Dispose();
                XButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private bool CheckNewKeyConflicts(InputEvent @event, InputGroupList groupList, SpecifiedInputKey? old)
    {
        // Get the conflicts with the new input.
        var conflict = groupList.Conflicts(this);
        if (conflict != null)
        {
            AssociatedEvent = old;

            // If there are conflicts detected reset the changes and ask the user.
            groupList.ShowInputConflictDialog(this, conflict, @event);
            return true;
        }

        var associatedAction = Action;

        if (associatedAction?.Inputs == null)
        {
            GD.PrintErr("Can't check conflicts because associated action or its inputs is null");
            return false;
        }

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

        // Alternative button content doesn't need to become visible as it will be recreated in UpdateButtonText

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

        if (groupList == null)
        {
            GD.PrintErr($"Can't handle button press in {nameof(InputEventItem)} due to missing group list");
            return;
        }

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

        if (alternativeButtonContentToText != null)
            alternativeButtonContentToText.Visible = false;

        // Notify InputManager that input rebinding has started and it should not react to input
        InputManager.PerformingRebind = true;
    }

    private void UpdateButtonText()
    {
        xButton.Visible = false;

        if (alternativeButtonContentToText != null)
        {
            alternativeButtonContentToText.QueueFree();
            alternativeButtonContentToText = null;
        }

        if (AssociatedEvent == null)
        {
            button.Text = "error";
            button.HintTooltip = string.Empty;
            return;
        }

        UpdateButtonContentFromEvent();
    }

    private void UpdateButtonContentFromEvent()
    {
        if (AssociatedEvent!.PrefersGraphicalRepresentation)
        {
            button.Text = string.Empty;

            alternativeButtonContentToText = AssociatedEvent.GenerateGraphicalRepresentation();

            button.AddChild(alternativeButtonContentToText);

            button.RectMinSize = new Vector2(alternativeButtonContentToText.RectSize.x + 1,
                alternativeButtonContentToText.RectSize.y + 1);

            // To guard against broken inputs being entirely unknown, show the name of the key on hover
            button.HintTooltip = AssociatedEvent.ToString();
        }
        else
        {
            button.Text = AssociatedEvent.ToString();
            button.RectMinSize = new Vector2(0, 0);
            button.HintTooltip = string.Empty;
        }
    }

    private void OnIconsChanged(object sender, EventArgs eventArgs)
    {
        if (AssociatedEvent != null && alternativeButtonContentToText != null)
        {
            UpdateButtonContentFromEvent();
        }
    }

    private void OnControllerChanged(object sender, EventArgs eventArgs)
    {
        // This avoids duplicate refresh with the general icons changed signal
        if (KeyPromptHelper.InputMethod == ActiveInputMethod.Controller)
            return;

        OnIconsChanged(sender, eventArgs);
    }
}
