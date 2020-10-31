using System;
using Godot;
using Newtonsoft.Json;

public class InputEventItem : Node
{
    [Export]
    public NodePath ButtonPath;

    internal bool JustAdded;

    private Button button;

    public bool WaitingForInput { get; private set; }

    [JsonProperty]
    [JsonConverter(typeof(InputEventWithModifiersConverter))]
    public InputEventWithModifiers AssociatedEvent { get; set; }

    [JsonIgnore]
    internal InputActionItem AssociatedAction { get; set; }

    public void Delete()
    {
        AssociatedAction.Inputs.Remove(this);
        InputGroupList.Instance.ControlsChanged();
    }

    public override void _Input(InputEvent @event)
    {
        if (InputGroupList.Instance.IsConflictDialogOpen())
            return;

        if (!WaitingForInput)
            return;

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

        var old = AssociatedEvent;
        AssociatedEvent = (InputEventWithModifiers)@event;
        var conflict = InputGroupList.Instance.Conflicts(this);
        if (conflict != null)
        {
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
        InputGroupList.Instance.ControlsChanged();

        UpdateButtonText();
    }

    public override void _Ready()
    {
        button = GetNode<Button>(ButtonPath);
        if (JustAdded)
            OnButtonPressed();
        else
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

    private string AsText()
    {
        var text = string.Empty;
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
