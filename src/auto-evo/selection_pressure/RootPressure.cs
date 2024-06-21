namespace AutoEvo;

using System.Collections.Generic;
using System.Linq;
using Godot;

public class RootPressure : SelectionPressure
{
    public static readonly LocalizedString Name = new LocalizedString("ROOT_PRESSURE");
    public RootPressure() : base(
        0,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            // Add a little bit of randomness to the miche tree
            new AddOrganelleAnywhere(_ => true),
            new RemoveAnyOrganelle(),
        },
        0)
    { }
    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return 1;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
