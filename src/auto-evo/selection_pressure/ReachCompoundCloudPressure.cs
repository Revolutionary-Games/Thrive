namespace AutoEvo;

public class ReachCompoundCloudPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("REACH_COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    private readonly float weight;

    public ReachCompoundCloudPressure(float weight) : base(weight,
        [
            new LowerRigidity(),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
        ],
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
