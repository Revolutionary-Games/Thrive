using Godot;

/// <summary>
///   A page in the Thriveopedia containing information about an organelle.
/// </summary>
public class ThriveopediaOrganellePage : ThriveopediaWikiPage
{
    [Export]
    public NodePath InfoBoxPath = null!;

    private OrganelleInfoBox infoBox = null!;

    public override string ParentPageName => "OrganellesRoot";

    /// <summary>
    ///   The organelle to display wiki information for.
    /// </summary>
    public OrganelleDefinition Organelle { get; set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        infoBox = GetNode<OrganelleInfoBox>(InfoBoxPath);

        infoBox.Organelle = Organelle;
    }
}
