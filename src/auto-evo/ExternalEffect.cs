using System;
using Newtonsoft.Json;

/// <summary>
///   Population effect external to the auto-evo simulation
/// </summary>
public class ExternalEffect
{
    public ExternalEffect(Species species, int constant, float coefficient, string eventType, Patch patch)
    {
        if (string.IsNullOrEmpty(eventType))
            throw new ArgumentException("May not be empty or null", nameof(eventType));

        Species = species;
        Constant = constant;
        Coefficient = coefficient;
        EventType = eventType;
        Patch = patch;
    }

    [JsonProperty]
    public Species Species { get; }

    [JsonProperty]
    public int Constant { get; set; }

    [JsonProperty]
    public float Coefficient { get; set; }

    [JsonProperty]
    public string EventType { get; set; }

    /// <summary>
    ///   The patch this effect affects.
    /// </summary>
    [JsonProperty]
    public Patch Patch { get; set; }
}
