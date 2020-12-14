using Newtonsoft.Json;

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

    [JsonProperty]
    public Species Species { get; }

    [JsonProperty]
    public int Constant { get; set; }

    [JsonProperty]
    public float Coefficient { get; set; }

    [JsonProperty]
    public string EventType { get; set; }
}
