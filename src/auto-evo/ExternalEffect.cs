using System;

/// <summary>
///   Population effect external to the auto-evo simulation
/// </summary>
public class ExternalEffect
{
    public ExternalEffect(Species species, int amount, string eventType)
    {
        Species = species;
        Amount = amount;
        EventType = eventType;
    }

    public Species Species { get; }
    public int Amount { get; set; }
    public string EventType { get; set; }
}
