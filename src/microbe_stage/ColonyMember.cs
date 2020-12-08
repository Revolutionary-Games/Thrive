using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

[JsonConverter(typeof(ColonyMemberSerializer))]
public class ColonyMember
{
    /// <summary>
    ///   Used for serialization. Should not be used otherwise
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

    [JsonIgnore]
    internal ColonyMember Master { get; set; }
    [JsonProperty]
    internal Vector3? OffsetToMaster { get; set; }
    [JsonProperty]
    internal List<ColonyMember> BindingTo { get; set; }
    [JsonIgnore]
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
