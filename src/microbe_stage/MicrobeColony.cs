using System;
using System.Collections.Generic;
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

    public void Process(float delta)
    {
        _ = delta; // Disable parameter not used suggestion

        ColonyCompounds.DistributeCompoundSurplus();
    }

    public void RemoveFromColony(Microbe microbe)
    {
        if (microbe?.Colony == null)
            throw new ArgumentException("Microbe null or invalid");

        if (!Equals(microbe.Colony, this))
            throw new ArgumentException("Cannot remove a colony member who isn't a member");

        ColonyMembers.ForEach(m => m.OnColonyMemberRemoved(microbe));

        ColonyMembers.Remove(microbe);

        while (microbe.ColonyChildren.Any())
            RemoveFromColony(microbe.ColonyChildren[0]);

        microbe.ColonyParent?.ColonyChildren?.Remove(microbe);

        microbe.Colony = null;
        microbe.ColonyParent = null;
        microbe.ColonyChildren = null;

        if (State == Microbe.MicrobeState.Unbinding)
            State = Microbe.MicrobeState.Normal;
    }

    public void AddToColony(Microbe microbe, Microbe master)
    {
        if (microbe == null || master == null || microbe.Colony != null)
            throw new ArgumentException("Microbe or master null");

        ColonyMembers.Add(microbe);

        microbe.ColonyParent = master;
        master.ColonyChildren.Add(microbe);
        microbe.Colony = this;
        microbe.ColonyChildren = new List<Microbe>();

        ColonyMembers.ForEach(m => m.OnColonyMemberAdded(microbe));
    }
}
