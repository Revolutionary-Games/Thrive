﻿using System.Collections.Generic;

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
    public PhagocytosisPhase PhagocytizedStep { get; set; }

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
    ///   Called once when this engulfable has been completely internalized by a microbe.
    /// </summary>
    void OnIngestedFromEngulfment();

    /// <summary>
    ///   Called once when this engulfable has been expelled by a microbe.
    /// </summary>
    void OnExpelledFromEngulfment();
}
