using Godot;

public class ThriveopediaOrganellePage : ThriveopediaWikiPage
{
    [Export]
    public NodePath InfoBoxPath = null!;

    private OrganelleInfoBox infoBox = null!;

    public override string Url => WikiPage.Url;
    public override string PageName => Organelle.InternalName;
    public override string TranslatedPageName => Organelle.Name;
    public override string? ParentPageName => null;

    public GameWiki.OrganelleWikiPage WikiPage { get; set; } = null!;

    public OrganelleDefinition Organelle { get; set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        infoBox = GetNode<OrganelleInfoBox>(InfoBoxPath);

        infoBox.Organelle = Organelle;

        AddSection(null, WikiPage.Sections.Description);
        AddSection("Requirements", WikiPage.Sections.Requirements);
    }
}
