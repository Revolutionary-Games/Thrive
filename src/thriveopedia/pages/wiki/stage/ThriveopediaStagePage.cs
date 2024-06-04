using Godot;

/// <summary>
///   A page in the Thriveopedia containing information about a stage.
/// </summary>
public partial class ThriveopediaStagePage : ThriveopediaWikiPage
{
    [Export]
    public NodePath? InfoBoxPath;

#pragma warning disable CA2213
    private StageInfoBox infoBox = null!;
#pragma warning restore CA2213

    public override string ParentPageName => "StagesRoot";

    public override void _Ready()
    {
        base._Ready();

        infoBox = GetNode<StageInfoBox>(InfoBoxPath);

        infoBox.Page = PageContent;
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
