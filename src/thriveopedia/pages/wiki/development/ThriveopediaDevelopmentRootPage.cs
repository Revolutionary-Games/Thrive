﻿/// <summary>
///   Parent page for development pages in the Thriveopedia. Content comes from the development category in the online
///   wiki.
/// </summary>
public partial class ThriveopediaDevelopmentRootPage : ThriveopediaWikiPage
{
    public override string PageName => "DevelopmentRoot";

    public override string TranslatedPageName => Localization.Translate("WIKI_DEVELOPMENT");

    public override string ParentPageName => "WikiRoot";

    public override bool StartsCollapsed => true;
}
