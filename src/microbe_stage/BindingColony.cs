using System;
using System.Collections.Generic;
using System.Linq;

public class BindingColony : IDisposable
{
    private ColonyMember leader;

    public BindingColony(Microbe leader)
    {
        this.leader = new ColonyMember(leader);
    }

    ~BindingColony()
    {
        Dispose(false);
    }

    public event EventHandler OnColonyDiscontinued;

    public IEnumerable<Microbe> Members => leader.GetAllMembers().Select(p => (Microbe)p);

    public IEnumerable<Microbe> GetMyBindingTargets(Microbe microbe)
    {
        return leader.GetMember(microbe).BindingTo.Select(p => (Microbe)p);
    }

    public Microbe GetMyMaster(Microbe microbe)
    {
        if (leader.MicrobeEquals(microbe))
            return null;

        return (Microbe)leader.GetMaster(microbe);
    }

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
            // Currently the colony gets destroyed
            Dispose();
        }

        return leader.RemoveMember(microbe);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (leader == null)
            return;

        if (disposing)
        {
            OnColonyDiscontinued?.Invoke(this, new EventArgs());
        }

        leader = null;
    }

    private class ColonyMember
    {
        private readonly Microbe microbe;

        internal ColonyMember(Microbe microbe)
        {
            this.microbe = microbe;
            BindingTo = new List<ColonyMember>();
        }

        internal List<ColonyMember> BindingTo { get; }

        public static explicit operator Microbe(ColonyMember m) => m.microbe;

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

        internal ColonyMember GetMaster(Microbe searchedMicrobe, ICollection<ColonyMember> visitedMicrobes = null)
        {
            (visitedMicrobes ??= new List<ColonyMember>()).Add(this);
            foreach (var currentMicrobeNeighbour in BindingTo)
            {
                if (!visitedMicrobes.Contains(currentMicrobeNeighbour))
                {
                    if (currentMicrobeNeighbour.MicrobeEquals(searchedMicrobe))
                        return this;

                    var res = currentMicrobeNeighbour.GetMaster(searchedMicrobe, visitedMicrobes);
                    if (res != null)
                        return res;
                }
            }

            return null;
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
