using Godot;

/// <summary>
///   Parent page for concept pages in the Thriveopedia. Content comes from the concepts category in the online
///   wiki.
/// </summary>
public class ThriveopediaConceptsRootPage : ThriveopediaWikiPage
{
    public override string PageName => "ConceptsRoot";

    public override string TranslatedPageName => TranslationServer.Translate("CONCEPTS");

    public override string ParentPageName => "WikiRoot";

    public override bool StartsCollapsed => false;
}
