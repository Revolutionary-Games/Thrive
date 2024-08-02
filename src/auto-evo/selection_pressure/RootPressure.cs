namespace AutoEvo;

public class RootPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("MICHE_ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public RootPressure() : base(1, [
        new RemoveOrganelle(_ => true),
        new AddOrganelleAnywhere(_ => true),
        new AddOrganelleAnywhere(organelle => organelle.InternalName == "nucleus"),
    ])
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
