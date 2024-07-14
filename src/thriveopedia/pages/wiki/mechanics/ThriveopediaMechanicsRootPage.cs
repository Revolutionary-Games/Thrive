using Godot;

/// <summary>
///   Parent page for mechanic pages in the Thriveopedia. Content comes from the mechanics category in the online
///   wiki.
/// </summary>
public partial class ThriveopediaMechanicsRootPage : ThriveopediaWikiPage
{
    [Signal]
    public delegate void OnStageChangedEventHandler();

    public override string PageName => "MechanicsRoot";

    public override string TranslatedPageName => TranslationServer.Translate("MECHANICS");

    public override string ParentPageName => "CurrentStage";

    public override bool StartsCollapsed => false;

    public override void OnSelectedStageChanged()
    {
        EmitSignal(SignalName.OnStageChanged);
    }
}
