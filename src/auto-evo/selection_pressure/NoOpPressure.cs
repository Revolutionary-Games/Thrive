namespace AutoEvo;

/// <summary>
///   This pressure does nothing, but is used as a placeholder node in the Miche Tree
/// </summary>
public class NoOpPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_NO_OP_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public NoOpPressure() : base(1, [])
    {
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        return 1;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
