using Godot;

public class ThriveopediaMuseumPage : ThriveopediaPage
{
    public override string PageName => "Museum";
    public override string TranslatedPageName => TranslationServer.Translate("MUSEUM_PAGE");

    public override void UpdateCurrentWorldDetails()
    {
    }
}