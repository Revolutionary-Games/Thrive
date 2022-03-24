using System.Collections.Generic;

public interface IEngulfable : IEntity
{
    /// <summary>
    ///   The size of this engulfable object based on microbe hex count.
    /// </summary>
    float Size { get; }

    EntityReference<Microbe> HostileEngulfer { get; }

    bool IsBeingEngulfed { get; set; }

    bool IsIngested { get; set; }

    /// <summary>
    ///   The value for how much this engulfable has been digested in the range of 0-1,
    ///   where 1 is fully digested.
    /// </summary>
    float DigestionProgress { get; set; }

    Dictionary<Compound, float> CalculateDigestibleCompounds();

    void NotifyEngulfed();
}
