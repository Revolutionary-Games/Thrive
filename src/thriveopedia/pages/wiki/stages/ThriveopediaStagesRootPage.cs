using Godot;

/// <summary>
///   Parent page for stage pages in the Thriveopedia. Content comes from the stages category in the online
///   wiki. Also contains a grid of buttons linking to all stage pages.
/// </summary>
public partial class ThriveopediaStagesRootPage : ThriveopediaWikiPage
{
    [Export]
    public NodePath? StageListContainerPath;

#pragma warning disable CA2213
    private PackedScene linkButtonScene = null!;
    private HFlowContainer stageListContainer = null!;
#pragma warning restore CA2213

    public override string PageName => "StagesRoot";

    public override string TranslatedPageName => TranslationServer.Translate("STAGES");

    public override string ParentPageName => "WikiRoot";

    public override bool StartsCollapsed => true;

    public override void _Ready()
    {
        base._Ready();

        stageListContainer = GetNode<HFlowContainer>(StageListContainerPath);
        linkButtonScene = GD.Load<PackedScene>("res://src/thriveopedia/pages/wiki/IconPageLinkButton.tscn");
    }

    public override void OnThriveopediaOpened()
    {
        base.OnThriveopediaOpened();

        var wiki = SimulationParameters.Instance.GetWiki();

        foreach (var stage in wiki.Stages)
        {
            var button = (IconPageLinkButton)linkButtonScene.Instantiate();

            button.DisplayName = stage.Name;
            button.IconPath = GetStageIconPath(stage);
            button.PageName = stage.InternalName;
            stageListContainer.AddChild(button);
        }
    }

    public override void OnSelectedStageChanged()
    {
        foreach (var obj in stageListContainer.GetChildren())
        {
            if (obj is IconPageLinkButton button)
            {
                var page = (ThriveopediaWikiPage)ThriveopediaManager.GetPage(button.PageName);
                button.Visible = page.VisibleInTree;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StageListContainerPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private string GetStageIconPath(GameWiki.Page page)
    {
        return page.InternalName switch
        {
            "microbe_stage" => "res://assets/textures/gui/bevel/parts/membraneAmoeba.png",
            "multicellular_stage" => "res://assets/textures/gui/bevel/multicellularTimelineMembraneTouch.png",
            _ => "res://assets/textures/gui/bevel/ProcessProcessing.png",
        };
    }
}
