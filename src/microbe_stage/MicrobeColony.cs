using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
public class MicrobeColony
{
    [JsonIgnore]
    private List<Microbe> colonyMemberCache;

    private Microbe.MicrobeState state;

    public MicrobeColony(Microbe master)
    {
        Master = master;
        master.ColonyChildren = new List<Microbe>();
        colonyMemberCache = new List<Microbe> { master };
        ColonyBag = new ColonyCompoundBag(this);
        OnMembersChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, master));
    }

    public event EventHandler<CollectionChangeEventArgs> OnMembersChanged;

    [JsonProperty]
    public ColonyCompoundBag ColonyBag { get; set; }

    [JsonProperty]
    public Microbe.MicrobeState State
    {
        get => state;
        set
        {
            if (state == value)
                return;

            state = value;
            foreach (var cell in GetColonyMembers())
                cell.State = value;
        }
    }

    [JsonProperty]
    public Microbe Master { get; set; }

    public List<Microbe> GetColonyMembers()
    {
        if (colonyMemberCache != null)
            return colonyMemberCache;

        colonyMemberCache = GetColonyMembers(Master, new List<Microbe>());
        return colonyMemberCache;
    }

    public void RemoveFromColony(Microbe microbe)
    {
        if (microbe?.Colony == null)
            throw new ArgumentException("Microbe null or invalid");

        if (!Equals(microbe.Colony, this))
            throw new ArgumentException("Cannot remove a colony member who isn't a member");

        OnMembersChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Remove, microbe));

        colonyMemberCache?.Remove(microbe);

        while (microbe.ColonyChildren.Any())
            RemoveFromColony(microbe.ColonyChildren[0]);

        microbe.ColonyParent?.ColonyChildren?.Remove(microbe);

        microbe.Colony = null;
        microbe.ColonyParent = null;
        microbe.ColonyChildren = null;
    }

    public void AddToColony(Microbe microbe, Microbe master)
    {
        if (microbe == null || master == null)
            throw new ArgumentException("Microbe or master null");

        colonyMemberCache?.Add(microbe);

        microbe.ColonyParent = master;
        master.ColonyChildren.Add(microbe);
        microbe.Colony = this;
        microbe.ColonyChildren = new List<Microbe>();

        OnMembersChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, microbe));
    }

    public bool AreInSameColony(Microbe a, Microbe b)
    {
        if (a.Colony == null || b.Colony == null)
            return false;

        return Equals(a.Colony, b.Colony);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (!(obj is MicrobeColony other))
            return false;

        return GetColonyMembers().SequenceEqual(other.GetColonyMembers());
    }

    public override int GetHashCode()
    {
        return GetColonyMembers().Aggregate(23, (a, b) => a ^ b.GetHashCode());
    }

    private List<Microbe> GetColonyMembers(Microbe current, List<Microbe> carry)
    {
        carry.Add(current);
        foreach (var child in current.ColonyChildren)
            GetColonyMembers(child, carry);
        return carry;
    }
}
