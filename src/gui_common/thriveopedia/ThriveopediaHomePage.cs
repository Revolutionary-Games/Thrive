using Godot;

public class ThriveopediaHomePage : ThriveopediaPage
{
    [Export]
    public NodePath CurrentWorldButtonPath = null!;

    private Button currentWorldButton = null!;

    public override string PageName => "HOME_PAGE";

    public override string TranslatedPageName => TranslationServer.Translate("HOME_PAGE");

    public override void _Ready()
    {
        base._Ready();

        currentWorldButton = GetNode<Button>(CurrentWorldButtonPath);

        UpdateCurrentWorldDetails();
    }

    public override void UpdateCurrentWorldDetails()
    {
        currentWorldButton.Disabled = CurrentGame == null;
    }

    private void OnCurrentWorldPressed()
    {
        OpenPage("CURRENT_WORLD_PAGE");
    }

    private void OnMuseumPressed()
    {
        OpenPage("MUSEUM_PAGE");
    }
}