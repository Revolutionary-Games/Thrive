using Godot;

/// <summary>
///   HUD for the society stage, manages updating the GUI for this stage
/// </summary>
public class SocietyHUD : StrategyStageHUDBase<SocietyStage>
{
    [Export]
    public NodePath? PopulationLabelPath;

#pragma warning disable CA2213
    private Label populationLabel = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnBuildingPlacingRequested();

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public override void _Ready()
    {
        base._Ready();

        populationLabel = GetNode<Label>(PopulationLabelPath);
    }

    public void OpenResearchScreen()
    {
        // TODO: implement this
        GD.Print("TODO: research screen");
    }

    public void ForwardBuildingPlacingRequest()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnBuildingPlacingRequested));
    }

    public void UpdatePopulationDisplay(long population)
    {
        populationLabel.Text = StringUtils.ThreeDigitFormat(population);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PopulationLabelPath?.Dispose();
        }

        base.Dispose(disposing);
    }
}
