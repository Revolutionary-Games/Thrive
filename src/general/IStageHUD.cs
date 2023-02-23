/// <summary>
///   Interface for <see cref="StageHUDBase{TStage}"/> to make complex inter dependencies work like with
///   <see cref="IEditor"/>
/// </summary>
public interface IStageHUD
{
    /// <summary>
    ///   Gets and sets the text that appears at the upper HUD.
    /// </summary>
    public string HintText { get; set; }

    public HUDMessages HUDMessages { get; }

    public void ShowPatchName(string localizedPatchName);
    public void ShowExtinctionBox();
    public void ShowPatchExtinctionBox();
    public void HidePatchExtinctionBox();
    public void OnEnterStageTransition(bool longerDuration, bool returningFromEditor);
    public void ShowReproductionDialog();
    public void HideReproductionDialog();
}
