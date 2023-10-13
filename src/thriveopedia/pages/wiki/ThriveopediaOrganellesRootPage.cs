using Godot;

/// <summary>
///   Parent page for organelle pages in the Thriveopedia. Content comes from the organelle category in the online wiki. Also contains a grid of buttons linking to all organelle pages.
/// </summary>
public class ThriveopediaOrganellesRootPage : ThriveopediaWikiPage
{
    [Export]
    public NodePath OrganelleListContainerPath = null!;

    private GridContainer organelleListContainer = null!;

    public override string PageName => "OrganellesRoot";

    public override string TranslatedPageName => TranslationServer.Translate("ORGANELLES");

    public override string? ParentPageName => "WikiRoot";

    public override bool StartsCollapsed => true;

    private PackedScene linkButtonScene = null!;

    public override void _Ready()
    {
        base._Ready();

        organelleListContainer = GetNode<GridContainer>(OrganelleListContainerPath);
        linkButtonScene = GD.Load<PackedScene>("res://src/thriveopedia/pages/wiki/OrganelleLinkButton.tscn");
    }

    public override void OnThriveopediaOpened()
    {
        base.OnThriveopediaOpened();

        var wiki = SimulationParameters.Instance.GetWiki();

        foreach (var organelle in wiki.Organelles)
        {
            var button = (OrganelleLinkButton)linkButtonScene.Instance();
            button.Organelle = SimulationParameters.Instance.GetOrganelleType(organelle.InternalName);
            button.OpenLink = () => ChangePage(organelle.InternalName);
            organelleListContainer.AddChild(button);
        }
    }
}
