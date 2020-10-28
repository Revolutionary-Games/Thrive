using System;
using Godot;
using Newtonsoft.Json;

public class InputEventItem : Node
{
    [Export]
    public NodePath ButtonPath;

    internal InputActionItem AssociatedAction;

    internal bool JustAdded;

    private Button button;

    private bool waitingForInput;

    [JsonProperty]
    [JsonConverter(typeof(InputEventWithModifiersConverter))]
    public InputEventWithModifiers AssociatedEvent { get; set; }

    public void Delete()
    {
        AssociatedAction.Inputs.Remove(this);
        AssociatedAction.AssociatedGroup.GetParent<InputGroupList>().ControlSchemeChanged();
    }

    public override void _Input(InputEvent @event)
    {
        if (InputGroupList.Instance.IsConflictDialogOpen())
            return;

        if (!waitingForInput)
            return;

        if (!(@event is InputEventMouseButton) && !(@event is InputEventKey))
            return;

        if (@event is InputEventKey iek)
        {
            switch (iek.Scancode)
            {
                case (uint)KeyList.Escape:
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
            InputGroupList.Instance.ShowDialog(this, conflict, AssociatedEvent);
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

        waitingForInput = false;
        JustAdded = false;
        AssociatedAction.AssociatedGroup.GetParent<InputGroupList>().ControlSchemeChanged();

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

    private void OnButtonPressed()
    {
        waitingForInput = true;
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
