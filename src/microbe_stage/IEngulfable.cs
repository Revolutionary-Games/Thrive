using System.Collections.Generic;

/// <summary>
///   Objects that can be engulfed by a microbe.
/// </summary>
[UseThriveSerializer]
public interface IEngulfable : IGraphicalEntity
{
    /// <summary>
    ///   The engulf size of this engulfable object.
    /// </summary>
    public float EngulfSize { get; }

    public float Radius { get; }

    public EntityReference<Microbe> HostileEngulfer { get; }

    /// <summary>
    ///   The current step of phagocytosis process this engulfable is currently in.
    ///   If not phagocytized, state is <see cref="PhagocytosisPhase.None"/>.
    /// </summary>
    public PhagocytosisPhase PhagocytosisStep { get; set; }

    /// <summary>
    ///   What specific enzyme needed to digest (break down) this engulfable. If null default is used (lipase).
    /// </summary>
    public Enzyme? RequisiteEnzymeToDigest { get; }

    public CompoundBag Compounds { get; }

    /// <summary>
    ///   The value for how much this engulfable has been digested in the range of 0 to 1,
    ///   where 1 means fully digested.
    /// </summary>
    public float DigestedAmount { get; set; }

    public Dictionary<Compound, float>? CalculateAdditionalDigestibleCompounds();

    /// <summary>
    ///   Called once when this engulfable is currently being attempted to be engulfed by a microbe.
    /// </summary>
    public void OnAttemptedToBeEngulfed();

    /// <summary>
    ///   Called once when this engulfable has been completely internalized by a microbe.
    /// </summary>
    public void OnIngestedFromEngulfment();

    /// <summary>
    ///   Called once when this engulfable has been expelled by a microbe.
    /// </summary>
    public void OnExpelledFromEngulfment();
}
