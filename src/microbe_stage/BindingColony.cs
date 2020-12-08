using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[UseThriveSerializer]
public class BindingColony
{
    [JsonProperty]
    private ColonyMember leader;

    /// <summary>
    ///   Used for json. Should not be used
    /// </summary>
    public BindingColony()
    {
    }

    public BindingColony(Microbe leader)
    {
        this.leader = new ColonyMember(leader, null);
    }

    [JsonIgnore]
    public Microbe Leader => (Microbe)leader;

    [JsonIgnore]
    public IEnumerable<Microbe> Members => leader.GetAllMembers().Select(p => (Microbe)p);

    public IEnumerable<Microbe> GetMyBindingTargets(Microbe microbe)
    {
        return leader.GetMember(microbe).BindingTo.Select(p => (Microbe)p);
    }

    public Microbe GetMyMaster(Microbe microbe)
    {
        if (leader.MicrobeEquals(microbe))
            return null;

        return (Microbe)leader.GetMember(microbe).Master;
    }

    public Vector3? GetOffsetToMaster(Microbe microbe)
    {
        if (leader.MicrobeEquals(microbe))
            return null;

        return leader.GetMember(microbe).OffsetToMaster;
    }

    public void AddToColony(Microbe binder, Microbe bound)
    {
        var binderMember = leader.GetMember(binder);
        binderMember.BindingTo.Add(new ColonyMember(bound, binderMember));
    }

    public bool RemoveFromColony(Microbe microbe)
    {
        if (leader.MicrobeEquals(microbe))
        {
            // TODO: Determine new leader
            foreach (var member in Members)
            {
                member.RemovedFromColony();
            }
        }

        var child = leader.GetMember(microbe);
        var parent = child.Master;
        ((Microbe)child).RemovedFromColony();
        return parent.BindingTo.Remove(child);
    }
}
