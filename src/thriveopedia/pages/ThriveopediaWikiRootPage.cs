using Godot;

/// <summary>
///   Root page for the wiki section of the Thriveopedia. Contains links to major pages within the wiki.
/// </summary>
public partial class ThriveopediaWikiRootPage : ThriveopediaPage
{
    public override string PageName => "WikiRoot";

    public override string TranslatedPageName => Localization.Translate("WIKI");

    public override string? ParentPageName => null;

    public override bool StartsCollapsed => true;

    public void OnOrganellesPressed()
    {
        ThriveopediaManager.OpenPage("OrganellesRoot");
    }
}
