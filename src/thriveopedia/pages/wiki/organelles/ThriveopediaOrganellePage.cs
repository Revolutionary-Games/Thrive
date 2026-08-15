using Godot;

/// <summary>
///   A page in the Thriveopedia containing information about an organelle.
/// </summary>
public partial class ThriveopediaOrganellePage : ThriveopediaWikiPage
{
#pragma warning disable CA2213
    [Export]
    private OrganelleInfoBox infoBox = null!;
#pragma warning restore CA2213

    public override string ParentPageName => "OrganellesRoot";

    /// <summary>
    ///   The organelle to display wiki information for.
    /// </summary>
    public OrganelleDefinition Organelle { get; set; } = null!;

    public override string TranslatedAdditionalSearchContent =>
        Localization.Translate("THRIVEOPEDIA_ORGANELLE_SEARCHTAGS")
        + "\n" + base.TranslatedAdditionalSearchContent;

    public override void _Ready()
    {
        base._Ready();

        infoBox.Organelle = Organelle;
    }
}
