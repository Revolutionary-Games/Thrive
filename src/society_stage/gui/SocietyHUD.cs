using System;
using Godot;

/// <summary>
///   HUD for the society stage, manages updating the GUI for this stage
/// </summary>
public class SocietyHUD : StrategyStageHUDBase<SocietyStage>
{
    [Export]
    public NodePath? PopulationLabelPath;

    [Export]
    public NodePath ResearchScreenPath = null!;

#pragma warning disable CA2213
    private Label populationLabel = null!;

    private ResearchScreen researchScreen = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnBuildingPlacingRequested();

    [Signal]
    public delegate void OnStartResearching(string technology);

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public override void _Ready()
    {
        base._Ready();

        populationLabel = GetNode<Label>(PopulationLabelPath);
        researchScreen = GetNode<ResearchScreen>(ResearchScreenPath);
    }

    public void OpenResearchScreen()
    {
        if (researchScreen.Visible)
        {
            researchScreen.Close();
        }
        else
        {
            researchScreen.AvailableTechnologies = stage?.CurrentGame?.TechWeb ??
                throw new InvalidOperationException("HUD not initialized");

            // This is not opened centered to allow the player to move the window and for that to be remembered
            researchScreen.Open();

            // TODO: update the hot bar state
        }
    }

    public void UpdateResearchProgress(TechnologyProgress? currentResearch)
    {
        researchScreen.DisplayProgress(currentResearch);
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
            if (PopulationLabelPath != null)
            {
                PopulationLabelPath.Dispose();
                ResearchScreenPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ResearchScreenClosed()
    {
        // TODO: update the hot bar state
    }

    private void ForwardStartResearch(string technology)
    {
        EmitSignal(nameof(OnStartResearching), technology);
    }
}
