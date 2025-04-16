using Godot;

/// <summary>
///   Parent page for organelle pages in the Thriveopedia. Content comes from the organelle category in the online
///   wiki. Also contains a grid of buttons linking to all organelle pages.
/// </summary>
public partial class ThriveopediaOrganellesRootPage : ThriveopediaWikiPage
{
#pragma warning disable CA2213
    private PackedScene linkButtonScene = null!;
    [Export]
    private HFlowContainer organelleListContainer = null!;
#pragma warning restore CA2213

    public override string PageName => "OrganellesRoot";

    public override string TranslatedPageName => Localization.Translate("ORGANELLES");

    public override string ParentPageName => "CurrentStage";

    public override bool StartsCollapsed => true;

    public override void _Ready()
    {
        base._Ready();

        linkButtonScene = GD.Load<PackedScene>("res://src/thriveopedia/pages/wiki/IconPageLinkButton.tscn");
    }

    public override void OnThriveopediaOpened()
    {
        base.OnThriveopediaOpened();

        var wiki = SimulationParameters.Instance.GetWiki();

        // Ensure duplicate buttons aren't created each time the page is opened
        // TODO: for more efficiency could update existing buttons instead
        organelleListContainer.QueueFreeChildren();

        foreach (var organelle in wiki.Organelles)
        {
            var button = linkButtonScene.Instantiate<IconPageLinkButton>();
            var organelleDefinition = SimulationParameters.Instance.GetOrganelleType(organelle.InternalName);
            button.IconPath = organelleDefinition.IconPath!;
            button.DisplayName = organelleDefinition.Name;
            button.PageName = organelle.InternalName;
            organelleListContainer.AddChild(button);
        }
    }

    public override void OnSelectedStageChanged()
    {
        foreach (var node in organelleListContainer.GetChildren())
        {
            if (node is IconPageLinkButton button)
            {
                var page = (ThriveopediaWikiPage)ThriveopediaManager.GetPage(button.PageName);

                // TODO: should this property be in the base page interface to avoid having the cast above?
                button.Visible = page.VisibleInTree;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
        }

        base.Dispose(disposing);
    }
}
