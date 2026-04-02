public interface IReadOnlyEnvironmentalTolerances
{
    public float PreferredTemperature { get; }
    public float TemperatureTolerance { get; }
    public float PressureMinimum { get; }
    public float PressureTolerance { get; }
    public float UVResistance { get; }
    public float OxygenResistance { get; }

    public EnvironmentalTolerances Clone()
    {
        var newTolerances = new EnvironmentalTolerances();
        newTolerances.CopyFrom(this);
        return newTolerances;
    }
}
