namespace AutoEvo;

using System.Collections.Generic;

public class NoOpPressure : SelectionPressure
{
    public static readonly LocalizedString Name = new LocalizedString("NO_OP_PRESSURE");
    public NoOpPressure() : base(
        0,
        new List<IMutationStrategy<MicrobeSpecies>> { },
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
