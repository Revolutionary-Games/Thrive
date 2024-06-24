using Godot;

/// <summary>
///   HUD for the industrial stage. Very similar to <see cref="SocietyHUD"/>
/// </summary>
public partial class IndustrialHUD : StrategyStageHUDBase<IndustrialStage>
{
    // TODO: merge the common parts with the society stage hud into its own sub-scenes
    [Export]
    public NodePath? PopulationLabelPath;

    [Export]
    public NodePath CityScreenPopupPath = null!;

#pragma warning disable CA2213
    private Label populationLabel = null!;

    private CityScreen cityScreenPopup = null!;
#pragma warning restore CA2213

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public override void _Ready()
    {
        base._Ready();

        populationLabel = GetNode<Label>(PopulationLabelPath);
        cityScreenPopup = GetNode<CityScreen>(CityScreenPopupPath);
    }

    public void UpdatePopulationDisplay(long population)
    {
        populationLabel.Text = StringUtils.ThreeDigitFormat(population);
    }

    public void OpenCityScreen(PlacedCity city)
    {
        cityScreenPopup.ShowForCity(city);
    }

    /// <summary>
    ///   Closes all open windows, called when something really important is being shown on screen
    /// </summary>
    public void CloseAllOpenWindows()
    {
        cityScreenPopup.Close();
        researchScreen.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PopulationLabelPath != null)
            {
                PopulationLabelPath.Dispose();
                CityScreenPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
