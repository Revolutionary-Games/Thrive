using Godot;

/// <summary>
///   HUD for the industrial stage. Very similar to <see cref="SocietyHUD"/>
/// </summary>
public partial class IndustrialHUD : StrategyStageHUDBase<IndustrialStage>
{
    // TODO: merge the common parts with the society stage hud into its own sub-scenes
#pragma warning disable CA2213
    [Export]
    private Label populationLabel = null!;

    [Export]
    private CityScreen cityScreenPopup = null!;
#pragma warning restore CA2213

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

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
}
