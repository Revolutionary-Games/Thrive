﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class MicrobeColony
{
    private Microbe.MicrobeState state;

    public MicrobeColony(Microbe master)
    {
        Master = master;
        master.ColonyChildren = new List<Microbe>();
        ColonyMembers = new List<Microbe> { master };
        ColonyCompounds = new ColonyCompoundBag(this);
    }

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

    [JsonProperty]
    public Microbe Master { get; private set; }

    public void Process(float delta)
    {
        _ = delta;

        ColonyCompounds.DistributeCompoundSurplus();
    }

    public void RemoveFromColony(Microbe microbe)
    {
        if (microbe?.Colony == null)
            throw new ArgumentException("Microbe null or not a member of a colony");

        if (!Equals(microbe.Colony, this))
            throw new ArgumentException("Cannot remove a colony member who isn't a member");

        if (State == Microbe.MicrobeState.Unbinding)
            State = Microbe.MicrobeState.Normal;

        foreach (var colonyMember in ColonyMembers)
            colonyMember.OnColonyMemberRemoved(microbe);

        microbe.Colony = null;

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
    }

    public void AddToColony(Microbe microbe, Microbe master)
    {
        if (microbe == null || master == null || microbe.Colony != null)
            throw new ArgumentException("Microbe or master null or microbe already is in a colony");

        ColonyMembers.Add(microbe);

        microbe.ColonyParent = master;
        master.ColonyChildren.Add(microbe);
        microbe.Colony = this;
        microbe.ColonyChildren = new List<Microbe>();

        ColonyMembers.ForEach(m => m.OnColonyMemberAdded(microbe));
    }
}
