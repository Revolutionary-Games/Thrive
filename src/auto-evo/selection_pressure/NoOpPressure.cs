namespace AutoEvo;

public class NoOpPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("MICHE_NO_OP_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public NoOpPressure() : base(1, [])
    {
    }

    public override float Score(Species species, SimulationCache cache)
    {
        return 1;
    }

    public override float GetEnergy()
    {
        return 0;
    }

    public override LocalizedString GetDescription()
    {
        return Name;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
