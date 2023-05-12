/// <summary>
///   Interface for <see cref="CreatureStageHUDBase{TStage}"/> to make complex inter dependencies work like with
///   <see cref="IEditor"/>
/// </summary>
public interface ICreatureStageHUD : IStageHUD
{
    /// <summary>
    ///   Gets and sets the text that appears at the upper HUD.
    /// </summary>
    public string HintText { get; set; }

    public void ShowPatchName(string localizedPatchName);
    public void ShowExtinctionBox();
    public void ShowPatchExtinctionBox();
    public void HidePatchExtinctionBox();
    public void ShowReproductionDialog();
    public void HideReproductionDialog();
}
