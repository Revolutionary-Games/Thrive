using Godot;

/// <summary>
///   Parent page for concept pages in the Thriveopedia. Content comes from the development category in the online
///   wiki.
/// </summary>
public class ThriveopediaDevelopmentRootPage : ThriveopediaWikiPage
{
    public override string PageName => "DevelopmentRoot";

    public override string TranslatedPageName => TranslationServer.Translate("DEVELOPMENT");

    public override string ParentPageName => "WikiRoot";

    public override bool StartsCollapsed => false;
}
