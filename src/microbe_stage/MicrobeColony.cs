﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class MicrobeColony
{
    private Microbe.MicrobeState state;

    private bool hexCountDirty = true;
    private float hexCount;

    [JsonConstructor]
    private MicrobeColony(Microbe master)
    {
        Master = master;
        master.ColonyChildren = new List<Microbe>();
        ColonyMembers = new List<Microbe> { master };
        ColonyCompounds = new ColonyCompoundBag(this);

        // Grab initial state from microbe to preserve that (only really important for multicellular)
        state = master.State;
    }

    /// <summary>
    ///   The colony lead cell. Needs to be before <see cref="ColonyMembers"/> for JSON deserialization to work
    /// </summary>
    [JsonProperty]
    public Microbe Master { get; private set; }

    /// <summary>
    ///   Returns all members of this colony including the colony leader.
    /// </summary>
    [JsonProperty]
    public List<Microbe> ColonyMembers { get; private set; }

    [JsonProperty]
    public ColonyCompoundBag ColonyCompounds { get; private set; }

    [JsonProperty]
    public Microbe.MicrobeState State
    {
        get => state;
        set
        {
            if (state == value)
                return;

            state = value;
            foreach (var cell in ColonyMembers)
                cell.State = value;
        }
    }

    /// <summary>
    ///   The total hex count from all members of this colony.
    /// </summary>
    [JsonIgnore]
    public float HexCount
    {
        get
        {
            if (hexCountDirty)
                UpdateHexCount();
            return hexCount;
        }
    }

    /// <summary>
    ///   The accumulation of all the colony member's <see cref="Microbe.UsedIngestionCapacity"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This unfortunately is not cached as <see cref="Microbe.UsedIngestionCapacity"/> can change
    ///     every frame.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public float UsedIngestionCapacity => ColonyMembers.Sum(c => c.UsedIngestionCapacity);

    /// <summary>
    ///   Creates a colony for a microbe, with the given microbe as the master,
    ///   and handles related updates (like microbe's colony and access to the editor button).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Should be used instead of the colony constructor, unless for loading from Json.
    ///   </para>
    /// </remarks>
    public static void CreateColonyForMicrobe(Microbe microbe)
    {
        microbe.Colony = new MicrobeColony(microbe);
        microbe.OnColonyMemberAdded(microbe);
    }

    public void Process(float delta)
    {
        _ = delta;

        ColonyCompounds.DistributeCompoundSurplus();
    }

    public void RemoveFromColony(Microbe? microbe)
    {
        if (microbe?.Colony == null)
            throw new ArgumentException("Microbe null or not a member of a colony");

        if (!Equals(microbe.Colony, this))
            throw new ArgumentException("Cannot remove a colony member who isn't a member");

        if (microbe.ColonyChildren == null)
            throw new ArgumentException("Invalid microbe with no colony children setup on it");

        if (State == Microbe.MicrobeState.Unbinding)
            State = Microbe.MicrobeState.Normal;

        foreach (var colonyMember in ColonyMembers)
            colonyMember.OnColonyMemberRemoved(microbe);

        microbe.Colony = null;

        microbe.ReParentShapes(microbe, Vector3.Zero);

        while (microbe.ColonyChildren.Count != 0)
            RemoveFromColony(microbe.ColonyChildren[0]);

        ColonyMembers.Remove(microbe);

        microbe.ColonyParent?.ColonyChildren?.Remove(microbe);

        if (microbe.ColonyParent?.Colony != null && microbe.ColonyParent?.ColonyParent == null &&
            microbe.ColonyParent?.ColonyChildren?.Count == 0)
        {
            RemoveFromColony(microbe.ColonyParent);
        }

        microbe.ColonyParent = null;
        microbe.ColonyChildren = null;
        if (microbe != Master)
            Master.Mass -= microbe.Mass;

        hexCountDirty = true;
    }

    public void AddToColony(Microbe microbe, Microbe master)
    {
        if (microbe == null || master == null || microbe.Colony != null)
            throw new ArgumentException("Microbe or master null or microbe already is in a colony");

        ColonyMembers.Add(microbe);
        Master.Mass += microbe.Mass;

        microbe.ColonyParent = master;
        master.ColonyChildren!.Add(microbe);
        microbe.Colony = this;
        microbe.ColonyChildren = new List<Microbe>();

        ColonyMembers.ForEach(m => m.OnColonyMemberAdded(microbe));

        hexCountDirty = true;
    }

    private void UpdateHexCount()
    {
        hexCount = 0;

        foreach (var member in ColonyMembers)
        {
            hexCount += member.EngulfSize;
        }
    }
}
