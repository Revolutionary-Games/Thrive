using Godot;

public partial class PlanetSettings : PanelContainer
{
    [Export]
    public OptionButton lifeOriginButton = null!;
    
    [Export]
    public OptionButton worldSizeButton = null!;

    [Export]
    public OptionButton worldTemperatureButton = null!;

    [Export]
    public OptionButton worldSeaLevelButton = null!;

    [Export]
    public OptionButton worldGeologicalActivityButton = null!;

    [Export]
    public OptionButton worldClimateInstabilityButton = null!;
}
