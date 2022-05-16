using System.Collections.Generic;

public enum EngulfmentStep
{
    NotEngulfed,
    BeingEngulfed,
    Ingested,
    Digested,
    FullyDigested,
    BeingRegurgitated,
    PreparingEjection,
}

[UseThriveSerializer]
public interface IEngulfable : IGraphicalEntity
{
    /// <summary>
    ///   The size of this engulfable object based on microbe hex count.
    /// </summary>
    public float Size { get; }

    public float Radius { get; }

    public EntityReference<Microbe> HostileEngulfer { get; }

    /// <summary>
    ///   The particular step of endocytosis process this engulfable is currently in.
    /// </summary>
    public EngulfmentStep CurrentEngulfmentStep { get; set; }

    /// <summary>
    ///   The value for how much this engulfable has been digested on the range of 0 to 1,
    ///   where 1 means fully digested.
    /// </summary>
    public float DigestionProgress { get; set; }

    Dictionary<Compound, float> CalculateDigestibleCompounds();

    /// <summary>
    ///   Called when this engulfable is being engulfed by a microbe.
    /// </summary>
    void OnEngulfed();

    /// <summary>
    ///   Called when this engulfable has been ejected/regurgitated by a microbe.
    /// </summary>
    void OnEjected();
}
