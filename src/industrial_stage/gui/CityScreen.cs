/// <summary>
///   Shows the info and controls for a single city
/// </summary>
public class CityScreen : CustomDialog
{
    private PlacedCity? managedCity;

    private float elapsed = 1;

    public override void _Process(float delta)
    {
        base._Process(delta);

        elapsed += delta;

        if (elapsed > 0.1f)
        {
            elapsed = 0;

            UpdateAllCityInfo();
        }
    }

    /// <summary>
    ///   Opens this screen for a city
    /// </summary>
    /// <param name="city">The city to open this for</param>
    public void ShowForCity(PlacedCity city)
    {
        if (Visible)
        {
            Close();
        }

        managedCity = city;

        UpdateAllCityInfo();
        Show();
    }

    private void UpdateAllCityInfo()
    {
        UpdateCityStats();
        UpdateAvailableBuildings();
        UpdateBuildQueue();
        UpdateConstructedBuildings();
    }

    private void UpdateCityStats()
    {
        WindowTitle = managedCity?.CityName ?? "ERROR";

        // TODO: update the bottom stats bar
    }

    private void UpdateAvailableBuildings()
    {
        // TODO: update this
    }

    private void UpdateBuildQueue()
    {
        // TODO: update this
    }

    private void UpdateConstructedBuildings()
    {
        // TODO: update this
    }
}
