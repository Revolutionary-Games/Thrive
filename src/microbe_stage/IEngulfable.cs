using System.Collections.Generic;

public interface IEngulfable : IEntity
{
    /// <summary>
    ///   The size of this engulfable object based on microbe hex count.
    /// </summary>
    public float Size { get; }

    public EntityReference<Microbe> HostileEngulfer { get; }

    public bool IsBeingEngulfed { get; set; }

    public bool IsIngested { get; set; }

    /// <summary>
    ///   The value for how much this engulfable has been digested on the range of 0 to 1,
    ///   where 1 means fully digested.
    /// </summary>
    public float DigestionProgress { get; set; }

    Dictionary<Compound, float> CalculateDigestibleCompounds();

    /// <summary>
    ///   Called when this engulfable has been ingested by a microbe.
    /// </summary>
    void OnEngulfed();
}
