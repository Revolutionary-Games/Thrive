using Godot;

/// <summary>
///   A page in the Thriveopedia containing information about an organelle.
/// </summary>
public class ThriveopediaOrganellePage : ThriveopediaWikiPage
{
    [Export]
    public NodePath? InfoBoxPath;

#pragma warning disable CA2213
    private OrganelleInfoBox infoBox = null!;
#pragma warning restore CA2213

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InfoBoxPath?.Dispose();
        }

        base.Dispose(disposing);
    }
}
