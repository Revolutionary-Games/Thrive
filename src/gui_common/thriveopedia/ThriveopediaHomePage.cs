using Godot;

public class ThriveopediaHomePage : ThriveopediaPage
{
    public override string PageName => "Home";
    public override string TranslatedPageName => TranslationServer.Translate("HOME_PAGE");

    public override void _Ready()
    {
        base._Ready();
    }

    public override void UpdateCurrentWorldDetails()
    {
    }
}