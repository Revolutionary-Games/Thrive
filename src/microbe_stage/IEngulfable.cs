using System.Collections.Generic;

[UseThriveSerializer]
public interface IEngulfable : IEntity
{
    /// <summary>
    ///   The size of this engulfable object based on microbe hex count.
    /// </summary>
    public float Size { get; }

    public float Radius { get; }

    public EntityReference<Microbe> HostileEngulfer { get; }

    public bool IsBeingIngested { get; set; }

    public bool IsBeingRegurgitated { get; set; }

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

    /// <summary>
    ///   Called when this engulfable has been ejected/regurgitated by a microbe.
    /// </summary>
    void OnEjected();
}
