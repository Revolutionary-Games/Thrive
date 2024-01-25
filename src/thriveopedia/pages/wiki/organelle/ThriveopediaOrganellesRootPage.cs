using Godot;

/// <summary>
///   Parent page for organelle pages in the Thriveopedia. Content comes from the organelle category in the online
///   wiki. Also contains a grid of buttons linking to all organelle pages.
/// </summary>
public class ThriveopediaOrganellesRootPage : ThriveopediaWikiPage
{
    [Export]
    public NodePath? OrganelleListContainerPath;

#pragma warning disable CA2213
    private PackedScene linkButtonScene = null!;
    private HFlowContainer organelleListContainer = null!;
#pragma warning restore CA2213

    public override string PageName => "OrganellesRoot";

    public override string TranslatedPageName => TranslationServer.Translate("ORGANELLES");

    public override string ParentPageName => "WikiRoot";

    public override bool StartsCollapsed => true;

    public override void _Ready()
    {
        base._Ready();

        organelleListContainer = GetNode<HFlowContainer>(OrganelleListContainerPath);
        linkButtonScene = GD.Load<PackedScene>("res://src/thriveopedia/pages/wiki/PageLinkButton.tscn");
    }

    public override void OnThriveopediaOpened()
    {
        base.OnThriveopediaOpened();

        var wiki = SimulationParameters.Instance.GetWiki();

        foreach (var organelle in wiki.Organelles)
        {
            var button = (PageLinkButton)linkButtonScene.Instance();
            var organelleDefinition = SimulationParameters.Instance.GetOrganelleType(organelle.InternalName);
            button.IconPath = organelleDefinition.IconPath!;
            button.DisplayName = organelleDefinition.Name;
            button.OpenLink = () => ThriveopediaManager.OpenPage(organelle.InternalName);
            organelleListContainer.AddChild(button);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OrganelleListContainerPath?.Dispose();
        }

        base.Dispose(disposing);
    }
}
