using Godot;

/// <summary>
///   HUD for the industrial stage. Very similar to <see cref="SocietyHUD"/>
/// </summary>
public class IndustrialHUD : StrategyStageHUDBase<IndustrialStage>
{
    // TODO: merge the common parts with the society stage hud into its own sub-scenes
    [Export]
    public NodePath? PopulationLabelPath;

#pragma warning disable CA2213
    private Label populationLabel = null!;
#pragma warning restore CA2213

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public override void _Ready()
    {
        base._Ready();

        populationLabel = GetNode<Label>(PopulationLabelPath);
        researchScreen = GetNode<ResearchScreen>(ResearchScreenPath);
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
