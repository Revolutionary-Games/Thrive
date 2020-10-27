using Godot;

public class InputEventItem : Node
{
    [Export]
    public NodePath LabelPath; // TODO: temp

    private Label label; // TODO: temp

    public InputEventWithModifiers AssociatedEvent { get; set; }

    public override void _Ready()
    {
        label = GetNode<Label>(LabelPath);
        label.Text = AssociatedEvent.AsText();
    }
}
