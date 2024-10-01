namespace AutoEvo;

using Newtonsoft.Json;

/// <summary>
///   This pressure does nothing, but is used as a placeholder node in the Miche Tree
/// </summary>
[JSONDynamicTypeAllowed]
public class NoOpPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_NO_OP_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public NoOpPressure() : base(1, [])
    {
    }

    [JsonIgnore]
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
