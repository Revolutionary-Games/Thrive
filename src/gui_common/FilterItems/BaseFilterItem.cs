using Godot;

public abstract class BaseFilterItem : HBoxContainer
{
    [Export]
    public NodePath ItemNameLabelPath = null!;

    private Label itemNameLabel = null!;

    public string ItemName { get => Name; set => itemNameLabel.Name = value; }

    public override void _Ready()
    {
        base._Ready();

        itemNameLabel = GetNode<Label>(ItemNameLabelPath);
    }

    private void OnDeleteButtonPressed()
    {
        QueueFree();
    }
}
