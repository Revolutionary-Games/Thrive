/// <summary>
///   Customized difficulty setting
/// </summary>
[CustomizedRegistryType]
public class CustomDifficulty : IDifficulty
{
    public float MPMultiplier { get; set; }
    public float AIMutationMultiplier { get; set; }
    public float CompoundDensity { get; set; }
    public float PlayerDeathPopulationPenalty { get; set; }
    public float GlucoseDecay { get; set; }
    public float OsmoregulationMultiplier { get; set; }
    public bool FreeGlucoseCloud { get; set; }
    public bool PassiveReproduction { get; set; }
    public bool LimitGrowthRate { get; set; }
    public FogOfWarMode FogOfWarMode { get; set; }
    public bool OrganelleUnlocksEnabled { get; set; }
}
