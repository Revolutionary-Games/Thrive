using Godot;

/// <summary>
///   A button to display the undiscovered organelles in each group.
/// </summary>
public class UndiscoveredOrganellesButton : MarginContainer
{
#pragma warning disable CA2213
    [Export]
    public ButtonGroup SelectionGroup = null!;

    private Button? button;
    private Label? countLabel;
#pragma warning restore CA2213

    private int count;

    public int Count
    {
        get => count;
        set
        {
            count = value;

            UpdateCount();
        }
    }

    public override void _Ready()
    {
        button = GetNode<Button>("VBoxContainer/Button");
        countLabel = GetNode<Label>("VBoxContainer/Button/Count");

        UpdateButton();
        UpdateCount();
    }

    private void UpdateCount()
    {
        if (countLabel == null)
            return;

        countLabel.Visible = count > 1;

        var text = new LocalizedString("N_TIMES", count);
        countLabel.Text = text.ToString();
    }

    private void UpdateButton()
    {
        if (button == null)
            return;

        button.Group = SelectionGroup;
    }
}
