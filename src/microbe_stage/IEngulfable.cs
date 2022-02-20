using System.Collections.Generic;

public interface IEngulfable
{
    /// <summary>
    ///   The size of this engulfable object based on microbe hex count.
    /// </summary>
    float Size { get; }

    EntityReference<Microbe> HostileEngulfer { get; }

    bool IsBeingEngulfed { get; set; }

    bool IsCompletelyEngulfed { get; set; }

    float DissolveEffectValue { get; set; }

    Dictionary<Compound, float> CalculateEngulfableCompounds();
}
