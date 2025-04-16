using Godot;

/// <summary>
///   HUD for the society stage, manages updating the GUI for this stage
/// </summary>
public partial class SocietyHUD : StrategyStageHUDBase<SocietyStage>
{
#pragma warning disable CA2213
    [Export]
    private Label populationLabel = null!;

#pragma warning restore CA2213

    [Signal]
    public delegate void OnBuildingPlacingRequestedEventHandler();

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public override void _Ready()
    {
        base._Ready();

    }

    public void ForwardBuildingPlacingRequest()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnBuildingPlacingRequested);
    }

    public void UpdatePopulationDisplay(long population)
    {
        populationLabel.Text = StringUtils.ThreeDigitFormat(population);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
        }

        base.Dispose(disposing);
    }
}
