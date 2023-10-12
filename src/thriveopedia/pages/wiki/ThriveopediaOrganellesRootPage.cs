using Godot;

// TODO get from wiki????
public class ThriveopediaOrganellesRootPage : ThriveopediaWikiPage
{
    [Export]
    public NodePath OrganelleListContainerPath = null!;

    private GridContainer organelleListContainer = null!;

    public override string PageName => "OrganellesRoot";

    // TODO translate
    public override string TranslatedPageName => "Organelles";

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
