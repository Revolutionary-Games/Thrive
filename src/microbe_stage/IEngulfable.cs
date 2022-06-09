using System.Collections.Generic;

/// <summary>
///   The endocytosis step.
/// </summary>
public enum EngulfmentStep
{
    /// <summary>
    ///   Default state for all engulfable objects.
    /// </summary>
    NotEngulfed,

    /// <summary>
    ///   Engulfable is in the process of being moved into the cytoplasm for storage.
    /// </summary>
    BeingEngulfed,

    /// <summary>
    ///   Engulfable has been moved into the cytoplasm and is completely internalized.
    /// </summary>
    Ingested,

    /// <summary>
    ///   Engulfable is completely broken down.
    /// </summary>
    Digested,

    /// <summary>
    ///   Engulfable is in the process of being moved into the membrane layer for ejection.
    /// </summary>
    BeingRegurgitated,

    /// <summary>
    ///   Intermediary step between the actual ejection for resetting this engulfable's size.
    /// </summary>
    PreparingEjection,
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
    ///   The particular step of endocytosis process this engulfable is currently in.
    /// </summary>
    public EngulfmentStep CurrentEngulfmentStep { get; set; }

    public bool Digestible { get; }

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
