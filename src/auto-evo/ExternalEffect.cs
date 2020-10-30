/// <summary>
///   Population effect external to the auto-evo simulation
/// </summary>
public class ExternalEffect
{
    public ExternalEffect(Species species, int constant, float coefficient, string eventType)
    {
        Species = species;
        Constant = constant;
        Coefficient = coefficient;
        EventType = eventType;
    }

    public Species Species { get; }
    public int Constant { get; set; }
    public float Coefficient { get; set; }
    public string EventType { get; set; }
}
