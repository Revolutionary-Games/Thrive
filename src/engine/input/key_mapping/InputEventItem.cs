using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Defines one specific event associated to an <see cref="InputActionItem">InputActionItem</see>.
/// </summary>
public class InputEventItem : Node
{
    [Export]
    public NodePath ButtonPath;

    private Button button;

    /// <summary>
    ///   If it is currently awaiting the user to press a button
    /// </summary>
    public bool WaitingForInput { get; private set; }

    /// <summary>
    ///   The currently assigned godot input event.
    ///   null if this event was just created
    /// </summary>
    [JsonProperty]
    [JsonConverter(typeof(InputEventWithModifiersConverter))]
    public InputEventWithModifiers AssociatedEvent { get; set; }

    /// <summary>
    ///   The action this event is associated with
    /// </summary>
    [JsonIgnore]
    internal InputActionItem AssociatedAction { get; set; }

    /// <summary>
    ///   If the event was just created and has never been assigned with a value
    /// </summary>
    [JsonIgnore]
    internal bool JustAdded { get; set; }

    /// <summary>
    ///   Deleted this event from the associated action and updated the godot InputMap
    /// </summary>
    public void Delete()
    {
        AssociatedAction.Inputs.Remove(this);
        InputGroupList.Instance.ControlsChanged();
    }

    public override void _Ready()
    {
        button = GetNode<Button>(ButtonPath);
        if (JustAdded)
            OnButtonPressed();
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
        if (InputGroupList.Instance.IsConflictDialogOpen())
            return;

        if (!WaitingForInput)
            return;

        // Only InputEventMouseButton and InputEventKey are supported now
        if (!(@event is InputEventMouseButton) && !(@event is InputEventKey))
            return;

        if (@event is InputEventKey iek)
        {
            switch (iek.Scancode)
            {
                case (uint)KeyList.Escape:
                    InputGroupList.Instance.WasListeningForInput = true;
                    Delete();
                    return;
                case (uint)KeyList.Alt:
                case (uint)KeyList.Shift:
                case (uint)KeyList.Control:
                    return;
            }
        }

        // The old godot input event. Null if this event is assigned a value the first time.
        var old = AssociatedEvent;
        AssociatedEvent = (InputEventWithModifiers)@event;

        // Get the conflicts with the new input.
        var conflict = InputGroupList.Instance.Conflicts(this);
        if (conflict != null)
        {
            // If there are conflicts detected reset the changes and ask the user.
            InputGroupList.Instance.ShowInputConflictDialog(this, conflict, AssociatedEvent);
            AssociatedEvent = old;
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
        InputGroupList.Instance.WasListeningForInput = false;

        // Update the godot InputMap
        InputGroupList.Instance.ControlsChanged();

        // Update the button text
        UpdateButtonText();
    }


    public override bool Equals(object obj)
    {
        if (!(obj is InputEventItem input))
            return false;

        return string.Equals(AsText(), input.AsText(), StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        return AsText().GetHashCode();
    }

    protected override void Dispose(bool disposing)
    {
        AssociatedAction = null;
        base.Dispose(disposing);
    }

    private void OnButtonPressed()
    {
        WaitingForInput = true;
        button.Text = "Press a key...";
    }

    private void UpdateButtonText()
    {
        button.Text = AsText();
    }

    /// <summary>
    ///   Creates a string for the button to show.
    /// </summary>
    /// <returns>
    ///   A human readable string.
    /// </returns>
    private string AsText()
    {
        var text = string.Empty;

        // Should never happen, just a fallback
        if (AssociatedEvent == null)
            return text;

        switch (AssociatedEvent)
        {
            case InputEventMouseButton iemb:
                if (AssociatedEvent.Control)
                    text += "Control+";
                if (AssociatedEvent.Alt)
                    text += "Alt+";
                if (AssociatedEvent.Shift)
                    text += "Shift+";
                text += iemb.ButtonIndex switch
                {
                    1 => "Left mouse",
                    2 => "Right mouse",
                    3 => "Middle mouse",
                    4 => "Wheel up",
                    5 => "Wheel down",
                    6 => "Wheel left",
                    7 => "Wheel right",
                    8 => "Special 1 mouse",
                    9 => "Special 2 mouse",
                    _ => throw new NotSupportedException($"Mouse button {iemb} not supported."),
                };
                break;
            case InputEventKey iek:
                text += iek.AsText();
                break;
            default:
                throw new NotSupportedException($"Input event {AssociatedEvent} is not supported");
        }

        return text;
    }
}
