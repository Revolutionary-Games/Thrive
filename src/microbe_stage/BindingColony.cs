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

    [UseThriveSerializer]
    private class ColonyMember
    {
        /// <summary>
        ///   Used for json. Should not be used
        /// </summary>
        public ColonyMember()
        {
        }

        internal ColonyMember(Microbe microbe, ColonyMember master)
        {
            Microbe = microbe;
            BindingTo = new List<ColonyMember>();
            Master = master;

            if (master != null)
            {
                OffsetToMaster = (((Microbe)master).Translation - microbe.Translation)
                    .Rotated(Vector3.Up, Mathf.Deg2Rad(-((Microbe)master).RotationDegrees.y));
            }
        }

        [JsonProperty]
        internal ColonyMember Master { get; set; }
        [JsonProperty]
        internal Vector3? OffsetToMaster { get; set; }
        [JsonProperty]
        internal List<ColonyMember> BindingTo { get; set; }
        [JsonProperty]
        internal Microbe Microbe { get; set; }

        public static explicit operator Microbe(ColonyMember m)
        {
            return m?.Microbe;
        }

        public override int GetHashCode()
        {
            return Microbe.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ColonyMember cm))
                return false;

            return Microbe.Equals(cm.Microbe);
        }

        public bool MicrobeEquals(Microbe otherMicrobe)
        {
            return Microbe.Equals(otherMicrobe);
        }

        internal ColonyMember GetMember(Microbe searchedMicrobe, ICollection<ColonyMember> visitedMicrobes = null)
        {
            if (MicrobeEquals(searchedMicrobe))
                return this;

            (visitedMicrobes ??= new List<ColonyMember>()).Add(this);
            foreach (var currentMicrobeNeighbour in BindingTo)
            {
                if (!visitedMicrobes.Contains(currentMicrobeNeighbour))
                {
                    var res = currentMicrobeNeighbour.GetMember(searchedMicrobe, visitedMicrobes);
                    if (res != null)
                        return res;
                }
            }

            return null;
        }

        internal ICollection<ColonyMember> GetAllMembers(ICollection<ColonyMember> visitedMicrobes = null)
        {
            (visitedMicrobes ??= new List<ColonyMember>()).Add(this);
            foreach (var colonyMember in BindingTo)
            {
                if (visitedMicrobes.Contains(colonyMember))
                    continue;

                colonyMember.GetAllMembers(visitedMicrobes);
            }

            return visitedMicrobes;
        }
    }
}
