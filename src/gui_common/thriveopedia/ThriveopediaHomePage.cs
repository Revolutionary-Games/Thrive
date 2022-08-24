using Godot;

public class ThriveopediaHomePage : ThriveopediaPage
{
    [Export]
    public NodePath CurrentWorldButtonPath = null!;

    private Button currentWorldButton = null!;

    private bool currentWorldDisabled = true;

    public override string PageName => "Home";

    public bool CurrentWorldDisabled
    {
        get => currentWorldDisabled;
        set
        {
            currentWorldDisabled = value;

            currentWorldButton.Disabled = CurrentWorldDisabled;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        currentWorldButton = GetNode<Button>(CurrentWorldButtonPath);
    }

    private void OnMuseumPressed()
    {
        OpenPage("Museum");
    }
}