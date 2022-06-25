using System.Collections.Generic;

/// <summary>
///   The ordered process of phagocytosis.
/// </summary>
public enum PhagocytosisProcess
{
    /// <summary>
    ///   Not being phagocytized in any way.
    /// </summary>
    None,

    /// <summary>
    ///   Engulfable is in the process of being moved into the cytoplasm to be stored.
    /// </summary>
    Ingestion,

    /// <summary>
    ///   Engulfable has been moved into the cytoplasm and is completely internalized. Digestion may begin.
    /// </summary>
    Ingested,

    /// <summary>
    ///   Engulfable is completely broken down.
    /// </summary>
    Digested,

    /// <summary>
    ///   Engulfable is in the process of being moved into the membrane layer for ejection.
    /// </summary>
    Exocytosis,

    /// <summary>
    ///   The expulsion of the engulfable into extracellular environment.
    /// </summary>
    Ejection,
}

/// <summary>
///   Objects that can be engulfed by a microbe.
/// </summary>
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
    ///   The current step of phagocytosis process this engulfable is currently in.
    /// </summary>
    public PhagocytosisProcess PhagocytizedStep { get; set; }

    /// <summary>
    ///   What specific enzyme needed to digest (break down) this engulfable. If null default is used (lipase).
    /// </summary>
    public Enzyme? RequisiteEnzymeToDigest { get; }

    public CompoundBag? Compounds { get; }

    /// <summary>
    ///   The value for how much this engulfable has been digested in the range of 0 to 1,
    ///   where 1 means fully digested.
    /// </summary>
    public float DigestedAmount { get; set; }

    Dictionary<Compound, float>? CalculateAdditionalDigestibleCompounds();

    /// <summary>
    ///   Called once when this engulfable is being engulfed by a microbe.
    /// </summary>
    void OnEngulfed();

    /// <summary>
    ///   Called once when this engulfable has been expelled by a microbe.
    /// </summary>
    void OnExpelled();
}
