public class ThriveopediaWikiRootPage : ThriveopediaPage
{
    public override string PageName => "WikiRoot";

    // TODO translate
    public override string TranslatedPageName => "Wiki";

    public override string? ParentPageName => null;

    public override bool StartsCollapsed => true;

    public void OnOrganellesPressed()
    {
        ChangePage("OrganellesRoot");
    }
}
