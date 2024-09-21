/// <summary>
///   Root page for the wiki section of the Thriveopedia. Contains links to major pages within the wiki.
/// </summary>
public partial class ThriveopediaWikiRootPage : ThriveopediaPage, IThriveopediaPage
{
    public string PageName => "WikiRoot";

    public string TranslatedPageName => Localization.Translate("WIKI");

    public string? ParentPageName => null;

    public override bool StartsCollapsed => true;

    public void OnOrganellesPressed()
    {
        ThriveopediaManager.OpenPage("OrganellesRoot");
    }

    public void OnStagesPressed()
    {
        // TODO: fix this
        // ThriveopediaManager.OpenPage("StagesRoot");

        // For now opens the microbe stage page as a workaround
        ThriveopediaManager.OpenPage("microbe_stage");
    }

    public void OnMechanicsPressed()
    {
        ThriveopediaManager.OpenPage("MechanicsRoot");
    }

    public void OnDevelopmentPressed()
    {
        ThriveopediaManager.OpenPage("DevelopmentRoot");
    }
}
