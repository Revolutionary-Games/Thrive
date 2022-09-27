using Godot;

public class ThriveopediaMuseumPage : ThriveopediaPage
{
    public override string PageName => "MUSEUM_PAGE";

    public override string TranslatedPageName => TranslationServer.Translate("MUSEUM_PAGE");

    public override void UpdateCurrentWorldDetails()
    {
    }
}