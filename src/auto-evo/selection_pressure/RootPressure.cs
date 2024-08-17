namespace AutoEvo;

public class RootPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public RootPressure() : base(1, [
        new RemoveOrganelle(_ => true),
        new AddOrganelleAnywhere(_ => true),
        new AddOrganelleAnywhere(organelle => organelle.InternalName == "nucleus"),
    ])
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
