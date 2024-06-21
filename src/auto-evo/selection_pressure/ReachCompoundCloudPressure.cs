namespace AutoEvo;

using System.Collections.Generic;

public class ReachCompoundCloudPressure : SelectionPressure
{
    public static readonly LocalizedString Name = new LocalizedString("REACH_COMPOUND_CLOUD_PRESSURE");
    private readonly float weight;
    public ReachCompoundCloudPressure(float weight) : base(
        weight,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            new LowerRigidity(),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
        },
        2000)
    {
        this.weight = weight;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return species.BaseSpeed * weight;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
