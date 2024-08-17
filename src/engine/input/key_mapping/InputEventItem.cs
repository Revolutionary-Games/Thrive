﻿using System;
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
public partial class InputEventItem : MarginContainer
{
    [Export]
    public NodePath? ButtonPath;

    [Export]
    public NodePath XButtonPath = null!;

    private readonly StringName uiSelectAction = new("ui_select");
    private readonly StringName uiAcceptAction = new("ui_accept");

#pragma warning disable CA2213
    private Button button = null!;
    private Button xButton = null!;
    private bool wasPressingButton;

    private Control? alternativeButtonContentToText;
#pragma warning restore CA2213

    /// <summary>
    ///   Keeps track of binding modifier keys directly as inputs and not as part of a combination
    /// </summary>
    private ModifierKeyMode modifierKeyStatus;

    private double timeSinceModifier;

    private enum ModifierKeyMode
    {
        None,
        ShiftPressed,
        AltPressed,
        CtrlPressed,
    }

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
        if (AssociatedEvent?.Code == (uint)Key.Escape)
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

    public override void _Process(double delta)
    {
        // Delayed key binding (to allow shift to be used in modifier combinations, for example)
        if (modifierKeyStatus == ModifierKeyMode.None)
            return;

        timeSinceModifier += delta;
        if (timeSinceModifier < Constants.MODIFIER_KEY_REBIND_DELAY)
            return;

        // Reset state first to prevent errors being able to trigger this multiple times in a row
        timeSinceModifier = 0;
        var keyToBind = modifierKeyStatus;
        modifierKeyStatus = ModifierKeyMode.None;

        GD.Print("Delay-binding a modifier key as an input");

        // TODO: allow controlling if physical keys or key labels should be used when rebinding by the user
        InputEventKey newInput;

        switch (keyToBind)
        {
            case ModifierKeyMode.ShiftPressed:
                newInput = new InputEventKey
                {
                    Keycode = Key.Shift,
                    Pressed = true,
                };
                break;
            case ModifierKeyMode.AltPressed:
                newInput = new InputEventKey
                {
                    Keycode = Key.Alt,
                    Pressed = true,
                };
                break;
            case ModifierKeyMode.CtrlPressed:
                newInput = new InputEventKey
                {
                    Keycode = Key.Ctrl,
                    Pressed = true,
                };
                break;

            default:
                GD.PrintErr("Unknown modifier key state");
                return;
        }

        var groupList = GroupList;

        if (groupList == null)
        {
            GD.PrintErr("InputEventItem has no group list");
            return;
        }

        if (groupList.IsConflictDialogOpen())
            return;

        try
        {
            var old = AssociatedEvent;
            AssociatedEvent = new SpecifiedInputKey(newInput, true);

            // Check conflicts, and don't proceed if there is a conflict
            if (CheckNewKeyConflicts(newInput, groupList, old))
                return;

            OnKeybindingSuccessfullyChanged();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to delay-bind a key: ", e);
        }
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
        if (@event is InputEventMidi or InputEventScreenDrag or InputEventScreenTouch or InputEventGesture
            or InputEventMouseMotion)
        {
            return;
        }

        // Hacky custom button press detection
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (xButton.IsHovered() && !xButton.Disabled)
            {
                GetViewport().SetInputAsHandled();

                Delete();

                // Rebind canceled, alert the InputManager so it can resume getting input
                InputManager.PerformingRebind = false;

                return;
            }

            if (button.IsHovered() && !WaitingForInput && mouseEvent.Pressed && !button.Disabled)
            {
                GetViewport().SetInputAsHandled();
                OnButtonPressed(mouseEvent);
                return;
            }
        }
        else if (!WaitingForInput && @event is InputEventJoypadButton joypadButton && button.HasFocus())
        {
            if (joypadButton.IsActionPressed(uiSelectAction))
            {
                // TODO: show somewhere in the GUI that this is for unbinding inputs

                GetViewport().SetInputAsHandled();
                OnButtonPressed(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Right,
                });
            }
            else if (joypadButton.IsActionPressed(uiAcceptAction))
            {
                GetViewport().SetInputAsHandled();
                OnButtonPressed(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
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

            switch (key.Keycode)
            {
                case Key.Escape:
                {
                    GetViewport().SetInputAsHandled();

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

                // Binding these are delayed operations to allow binding key combinations
                case Key.Alt:
                    modifierKeyStatus = ModifierKeyMode.AltPressed;
                    timeSinceModifier = 0;
                    return;
                case Key.Shift:
                    modifierKeyStatus = ModifierKeyMode.ShiftPressed;
                    timeSinceModifier = 0;
                    return;
                case Key.Ctrl:
                    modifierKeyStatus = ModifierKeyMode.CtrlPressed;
                    timeSinceModifier = 0;
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
            if (controllerAxis.Axis is JoyAxis.TriggerLeft or JoyAxis.TriggerRight)
                return;

            // TODO: controller device keybinding mode setting
            controllerAxis.Device = -1;
        }

        // If receiving another input to rebind, then queued modifier key rebinding is cancelled
        modifierKeyStatus = ModifierKeyMode.None;

        // The old key input event. Null if this event is assigned a value the first time.
        var old = AssociatedEvent;

        try
        {
            // TODO: allow controlling if physical keys or key labels should be used when rebinding by the user
            AssociatedEvent = new SpecifiedInputKey(@event, true);
        }
        catch (Exception e)
        {
            GD.PrintErr("Unbindable input got too far, error: ", e);
            return;
        }

        GetViewport().SetInputAsHandled();

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

        var result = (InputEventItem)associatedAction.GroupList.InputEventItemScene.Instantiate();

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

            uiSelectAction.Dispose();
            uiAcceptAction.Dispose();
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
            case MouseButton.Left:
                wasPressingButton = true;
                OnRebindButtonPressed();
                break;
            case MouseButton.Right:
                Delete();
                break;
        }
    }

    private void OnRebindButtonPressed()
    {
        WaitingForInput = true;
        button.Text = Localization.Translate("PRESS_KEY_DOT_DOT_DOT");
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
            button.TooltipText = string.Empty;
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

            button.CustomMinimumSize = new Vector2(alternativeButtonContentToText.Size.X + 1,
                alternativeButtonContentToText.Size.Y + 1);

            // To guard against broken inputs being entirely unknown, show the name of the key on hover
            button.TooltipText = AssociatedEvent.ToString();
        }
        else
        {
            button.Text = AssociatedEvent.ToString();
            button.CustomMinimumSize = new Vector2(0, 0);
            button.TooltipText = string.Empty;
        }
    }

    private void OnIconsChanged(object? sender, EventArgs eventArgs)
    {
        if (AssociatedEvent != null && alternativeButtonContentToText != null)
        {
            UpdateButtonContentFromEvent();
        }
    }

    private void OnControllerChanged(object? sender, EventArgs eventArgs)
    {
        // This avoids duplicate refresh with the general icons changed signal
        if (KeyPromptHelper.InputMethod == ActiveInputMethod.Controller)
            return;

        OnIconsChanged(sender, eventArgs);
    }
}
