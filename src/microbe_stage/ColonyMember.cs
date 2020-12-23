using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
public class ColonyMember
{
    /// <summary>
    ///   Used for serialization. Should not be used otherwise
    /// </summary>
    public ColonyMember()
    {
    }

    public ColonyMember(Microbe microbe, ColonyMember master)
    {
        Microbe = microbe;
        BindingTo = new List<ColonyMember>();
        Master = master;

        if (master != null)
        {
            var masterMicrobe = master.Microbe;
            OffsetToMaster = (masterMicrobe.Translation - microbe.Translation)
                .Rotated(Vector3.Up, Mathf.Deg2Rad(-masterMicrobe.RotationDegrees.y));
        }

        foreach (var member in Microbe.GetAllColonyMembers().Where(p => p != microbe))
        {
            member.Colony.OnColonyMembersChanged?.Invoke(this,
                new CollectionChangeEventArgs(CollectionChangeAction.Add, this));
        }

        OnColonyMembersChanged += (s, e) => AllMembersCache = null;
    }

    public event EventHandler OnRemovedFromColony;
    public event EventHandler<CollectionChangeEventArgs> OnColonyMembersChanged;

    [JsonProperty]
    public ColonyMember Master { get; set; }

    [JsonProperty]
    public Vector3? OffsetToMaster { get; set; }

    [JsonProperty]
    public List<ColonyMember> BindingTo { get; set; }

    [JsonProperty]
    public Microbe Microbe { get; set; }

    /// <summary>
    ///   Caches all the members of this colony.
    ///   Use Microbe.GetAllColonyMembers instead.
    /// </summary>
    [JsonIgnore]
    internal List<Microbe> AllMembersCache { get; set; }

    public static explicit operator Microbe(ColonyMember member)
    {
        return member?.Microbe;
    }

    public void RemoveFromColony()
    {
        var members = Microbe.GetAllColonyMembers();

        OnRemovedFromColony?.Invoke(this, new EventArgs());
        foreach (var microbe in members.Where(p => p != Microbe))
        {
            microbe.Colony.OnColonyMembersChanged?.Invoke(this,
                new CollectionChangeEventArgs(CollectionChangeAction.Remove, this));
        }

        Microbe = null;

        Master?.BindingTo.Remove(this);
        if (Master?.Microbe.GetAllColonyMembers().Count == 1)
            Master.RemoveFromColony();

        Master = null;
        OffsetToMaster = null;

        foreach (var colonyMember in BindingTo)
        {
            colonyMember.Master = null;

            // A colony alone doesn't make sense
            if (colonyMember.BindingTo.Count == 0)
            {
                colonyMember.RemoveFromColony();
            }
        }

        BindingTo = null;
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
                var member = currentMicrobeNeighbour.GetMember(searchedMicrobe, visitedMicrobes);
                if (member != null)
                    return member;
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
