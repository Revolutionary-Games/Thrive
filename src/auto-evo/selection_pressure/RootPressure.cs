namespace AutoEvo;

public class RootPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public RootPressure() : base(0,
        [
            new AddOrganelleAnywhere(_ => true), // Add a little bit of randomness to the miche tree
            new RemoveAnyOrganelle(),
        ],
        0)
    {
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return 1;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
