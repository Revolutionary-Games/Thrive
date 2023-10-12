using Godot;

public class ThriveopediaOrganellePage : ThriveopediaWikiPage
{
    [Export]
    public NodePath InfoBoxPath = null!;

    private OrganelleInfoBox infoBox = null!;

    public override string ParentPageName => "OrganellesRoot";

    public OrganelleDefinition Organelle { get; set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        infoBox = GetNode<OrganelleInfoBox>(InfoBoxPath);

        infoBox.Organelle = Organelle;
    }
}
