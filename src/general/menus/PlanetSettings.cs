using Godot;

/// <summary>
/// Planet settings in the planet customizer.
/// </summary>
public partial class PlanetSettings : VBoxContainer
{
    [Export]
    public OptionButton LifeOriginButton = null!;

    [Export]
    public OptionButton WorldSizeButton = null!;

    [Export]
    public OptionButton WorldTemperatureButton = null!;

    [Export]
    public OptionButton WorldSeaLevelButton = null!;

    [Export]
    public OptionButton WorldGeologicalActivityButton = null!;

    [Export]
    public OptionButton WorldClimateInstabilityButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LifeOriginButton?.Dispose();
            WorldSizeButton?.Dispose();
            WorldTemperatureButton?.Dispose();
            WorldSeaLevelButton?.Dispose();
            WorldGeologicalActivityButton?.Dispose();
            WorldClimateInstabilityButton?.Dispose();
        }

        base.Dispose(disposing);
    }
}
