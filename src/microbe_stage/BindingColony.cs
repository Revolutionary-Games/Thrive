using System.Collections.Generic;
using System.Linq;

public class BindingColony
{
    private ColonyMember leader;

    public BindingColony(Microbe leader)
    {
        this.leader = new ColonyMember(leader);
    }

    public IEnumerable<Microbe> Members => leader.GetAllMembers().Select(p => p.Microbe);

    public void AddToColony(Microbe binder, Microbe bound)
    {
        var binderMember = leader.GetMember(binder);
        binderMember.BindingTo.Add(new ColonyMember(bound));
    }

    public bool RemoveFromColony(Microbe microbe)
    {
        if (leader.MicrobeEquals(microbe))
        {
            // TODO: Determine new leader
        }

        return leader.RemoveMember(microbe);
    }

    private class ColonyMember
    {
        internal readonly Microbe Microbe;

        internal ColonyMember(Microbe microbe)
        {
            Microbe = microbe;
            BindingTo = new List<ColonyMember>();
        }

        internal List<ColonyMember> BindingTo { get; }

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

        internal bool RemoveMember(Microbe toRemove, ICollection<ColonyMember> visitedMicrobes = null)
        {
            (visitedMicrobes ??= new List<ColonyMember>()).Add(this);
            foreach (var colonyMember in BindingTo)
            {
                if (colonyMember.MicrobeEquals(toRemove))
                {
                    BindingTo.Remove(colonyMember);
                    return true;
                }

                if (colonyMember.RemoveMember(toRemove, visitedMicrobes))
                    return true;
            }

            return false;
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
