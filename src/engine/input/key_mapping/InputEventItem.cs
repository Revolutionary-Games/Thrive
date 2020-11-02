using Godot;

/// <summary>
///   Defines one specific event associated to an <see cref="InputActionItem">InputActionItem</see>.
/// </summary>
public class InputEventItem : Node
{
    [Export]
    public NodePath ButtonPath;

    [Export]
    public NodePath XButtonPath;

    private Button button;
    private Button xbutton;
    private bool wasPressingButton;

    /// <summary>
    ///   If it is currently awaiting the user to press a button
    /// </summary>
    public bool WaitingForInput { get; private set; }

    /// <summary>
    ///   The currently assigned input event.
    ///   null if this event was just created
    /// </summary>
    public SpecifiedInputKey AssociatedEvent { get; set; }

    /// <summary>
    ///   The action this event is associated with
    /// </summary>
    internal InputActionItem AssociatedAction { get; set; }

    /// <summary>
    ///   If the event was just created and has never been assigned with a value
    /// </summary>
    internal bool JustAdded { get; set; }

    /// <summary>
    ///   Delete this event from the associated action and updated the godot InputMap
    /// </summary>
    public void Delete()
    {
        AssociatedAction.Inputs.Remove(this);
        InputGroupList.ControlsChanged();
    }

    public override void _Ready()
    {
        button = GetNode<Button>(ButtonPath);
        xbutton = GetNode<Button>(XButtonPath);

        if (JustAdded)
            OnButtonLeftPressed();
        else
            UpdateButtonText();
    }

    /// <summary>
    ///   Performs the key reassigning.
    ///   Checks if it is waiting for a user input
    ///   Checks if there are any conflicts and opens the dialog if so
    ///   Overrides the old input with the new one
    ///   Update the godot InputMap
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (InputGroupList.IsConflictDialogOpen())
            return;

        if (!WaitingForInput)
            return;

        // Only InputEventMouseButton and InputEventKey are supported now
        if (!(@event is InputEventMouseButton) && !(@event is InputEventKey))
            return;

        if ((@event is InputEventMouseButton inputMouse) && inputMouse.ButtonIndex == (int)ButtonList.Left &&
            xbutton.IsHovered())
        {
            Delete();
            return;
        }

        if (wasPressingButton)
        {
            wasPressingButton = false;
            return;
        }

        if (@event is InputEventKey iek)
        {
            switch (iek.Scancode)
            {
                case (uint)KeyList.Escape:
                    if (AssociatedEvent == null)
                    {
                        Delete();
                        return;
                    }

                    InputGroupList.WasListeningForInput = true;
                    WaitingForInput = false;
                    UpdateButtonText();
                    return;
                case (uint)KeyList.Alt:
                case (uint)KeyList.Shift:
                case (uint)KeyList.Control:
                    return;
            }
        }

        // The old godot input event. Null if this event is assigned a value the first time.
        var old = AssociatedEvent;
        AssociatedEvent = new SpecifiedInputKey((InputEventWithModifiers)@event);

        // Get the conflicts with the new input.
        var conflict = InputGroupList.Conflicts(this);
        if (conflict != null)
        {
            AssociatedEvent = old;

            // If there are conflicts detected reset the changes and ask the user.
            InputGroupList.ShowInputConflictDialog(this, conflict, (InputEventWithModifiers)@event);
            return;
        }

        // Check if the input is already defined for this action
        // This code works by finding a pair
        for (var i = 0; i < AssociatedAction.Inputs.Count; i++)
        {
            for (var x = i + 1; x < AssociatedAction.Inputs.Count; x++)
            {
                if (AssociatedAction.Inputs[i].Equals(AssociatedAction.Inputs[x]))
                {
                    // Pair found (input already defined)
                    Delete();
                    return;
                }
            }
        }

        WaitingForInput = false;
        JustAdded = false;
        InputGroupList.WasListeningForInput = false;

        // Update the godot InputMap
        InputGroupList.ControlsChanged();

        // Update the button text
        UpdateButtonText();
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

    internal static InputEventItem BuildGUI(InputActionItem caller, SpecifiedInputKey @event)
    {
        var res = (InputEventItem)InputGroupList.InputEventItemScene.Instance();
        res.AssociatedAction = caller;
        res.AssociatedEvent = @event;
        return res;
    }

    protected override void Dispose(bool disposing)
    {
        AssociatedAction = null;
        button?.Dispose();
        xbutton?.Dispose();
        base.Dispose(disposing);
    }

    private void OnButtonPressed(InputEvent @event)
    {
        if (InputGroupList.IsConflictDialogOpen())
            return;

        if (InputGroupList.ListeningForInput)
            return;

        if (!(@event is InputEventMouseButton inputButton))
            return;

        switch (inputButton.ButtonIndex)
        {
            case (int)ButtonList.Left:
                wasPressingButton = true;
                OnButtonLeftPressed();
                break;
            case (int)ButtonList.Right:
                Delete();
                break;
        }
    }

    private void OnButtonLeftPressed()
    {
        WaitingForInput = true;
        button.Text = "Press a key...";
        xbutton.Visible = true;
    }

    private void UpdateButtonText()
    {
        button.Text = AssociatedEvent.ToString();
        xbutton.Visible = false;
    }
}
