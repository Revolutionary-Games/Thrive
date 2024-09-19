/// <summary>
///   Parent page for mechanic pages in the Thriveopedia. Content comes from the mechanics category in the online
///   wiki.
/// </summary>
public partial class ThriveopediaMechanicsRootPage : ThriveopediaWikiPage
{
    public override string PageName => "MechanicsRoot";

    public override string TranslatedPageName => Localization.Translate("WIKI_MECHANICS");

    public override string ParentPageName => "CurrentStage";

    public override bool StartsCollapsed => false;
}
