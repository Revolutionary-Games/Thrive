namespace AutoEvo;

using System;

public class RootPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("ROOT_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public RootPressure() : base(1, [
        new AddOrganelleAnywhere(_ => true), // Add a little bit of randomness to the miche tree
        new RemoveOrganelle(_ => true),
    ])
    {
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return 1;
    }

    public override float GetEnergy()
    {
        return 0;
    }

    public override IFormattable GetDescription()
    {
        // This shouldn't be called on 0 energy pressures
        return Name;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
