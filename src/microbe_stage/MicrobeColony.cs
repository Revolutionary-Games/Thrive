using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
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

    public event EventHandler<CollectionChangeEventArgs> OnMembersChanged;

    [JsonProperty]
    public List<Microbe> ColonyMembers { get; private set; }

    [JsonProperty]
    public ColonyCompoundBag ColonyCompounds { get; set; }

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
    public Microbe Master { get; set; }

    public void RemoveFromColony(Microbe microbe)
    {
        if (microbe?.Colony == null)
            throw new ArgumentException("Microbe null or invalid");

        if (!Equals(microbe.Colony, this))
            throw new ArgumentException("Cannot remove a colony member who isn't a member");

        OnMembersChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Remove, microbe));

        ColonyMembers.Remove(microbe);

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

        ColonyMembers.Add(microbe);

        microbe.ColonyParent = master;
        master.ColonyChildren.Add(microbe);
        microbe.Colony = this;
        microbe.ColonyChildren = new List<Microbe>();

        OnMembersChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, microbe));
    }
}
