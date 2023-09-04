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
    /// <remarks>
    ///   <para>
    ///     Some classes may scale this with <see cref="DigestedAmount"/> for richer gameplay depth.
    ///     For example see <see cref="Microbe.EngulfSize"/>.
    ///   </para>
    /// </remarks>
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

    /// <summary>
    ///   Additional means bonus compounds that can be acquired on top of <see cref="Compounds"/> from this engulfable
    ///   for predating microbes.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Use case: Say you want to engulf and digest an object, but it only has little resources stored in it, you
    ///     want to boost (or even add compounds to) this by specifying additional digestible compounds.
    ///     For example on how this is already used: In microbes every organelle has a build cost, which is ammonia and
    ///     phosphates. Most of the times, AI microbes have none of these so we want to reward players fairly by giving
    ///     them enough of these two.
    ///   </para>
    /// </remarks>
    /// <returns>The additional compounds, null if there's none.</returns>
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

    /// <summary>
    ///   The organelles the player can unlock when ingesting.
    /// </summary>
    public IEnumerable<OrganelleDefinition>? UnlocksOrganelles { get; }
}
