/// <summary>
///   Interface for <see cref="StageHUDBase{TStage,TPlayer}"/> to make complex inter dependencies work like with
///   <see cref="IEditor"/>
/// </summary>
public interface IStageHUD
{
    /// <summary>
    ///   Gets and sets the text that appears at the upper HUD.
    /// </summary>
    public string HintText { get; set; }

    public void UpdatePatchInfo(string localizedPatchName);
    public void ShowExtinctionBox();
    public void OnEnterStageTransition(bool longerDuration);
    public void ShowReproductionDialog();
    public void HideReproductionDialog();
}
