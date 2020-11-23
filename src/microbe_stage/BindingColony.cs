using System.Collections.Generic;
using System.Linq;
using Godot;

public class BindingColony
{
    private readonly ColonyMember leader;

    public BindingColony(Microbe leader)
    {
        this.leader = new ColonyMember(leader, null);
    }

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

    private class ColonyMember
    {
        internal readonly ColonyMember Master;
        internal readonly Vector3? OffsetToMaster;
        private readonly Microbe microbe;

        internal ColonyMember(Microbe microbe, ColonyMember master)
        {
            this.microbe = microbe;
            BindingTo = new List<ColonyMember>();
            Master = master;
            OffsetToMaster = microbe.Translation - ((Microbe)master)?.Translation;
        }

        internal List<ColonyMember> BindingTo { get; }

        public static explicit operator Microbe(ColonyMember m) => m?.microbe;

        public override int GetHashCode()
        {
            return microbe.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ColonyMember cm))
                return false;

            return microbe.Equals(cm.microbe);
        }

        public bool MicrobeEquals(Microbe otherMicrobe)
        {
            return microbe.Equals(otherMicrobe);
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
