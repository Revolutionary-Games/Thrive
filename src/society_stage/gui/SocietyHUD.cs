using Godot;

/// <summary>
///   HUD for the society stage, manages updating the GUI for this stage
/// </summary>
public class SocietyHUD : StrategyStageHUDBase<SocietyStage>
{
    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public void OpenResearchScreen()
    {
        // TODO: implement this
        GD.Print("TODO: research screen");
    }
}
