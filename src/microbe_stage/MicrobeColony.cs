using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class MicrobeColony
{
    private MicrobeState state;

    private bool membersDirty = true;
    private float hexCount;
    private bool canEngulf;
    private float entityWeight;

    [JsonConstructor]
    private MicrobeColony(Microbe master)
    {
        Master = master;
        master.ColonyChildren = new List<Microbe>();
        ColonyMembers = new List<Microbe> { master };
        ColonyCompounds = new ColonyCompoundBag(this);

        // Grab initial state from microbe to preserve that (only really important for multicellular)
        state = master.State;
    }

    /// <summary>
    ///   The colony lead cell. Needs to be before <see cref="ColonyMembers"/> for JSON deserialization to work
    /// </summary>
    [JsonProperty]
    public Microbe Master { get; private set; }

    /// <summary>
    ///   Returns all members of this colony including the colony leader.
    /// </summary>
    [JsonProperty]
    public List<Microbe> ColonyMembers { get; private set; }

    [JsonProperty]
    public ColonyCompoundBag ColonyCompounds { get; private set; }

    [JsonProperty]
    public MicrobeState State
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

    /// <summary>
    ///   The total hex count from all members of this colony.
    /// </summary>
    [JsonIgnore]
    public float HexCount
    {
        get
        {
            if (membersDirty)
                UpdateDerivedProperties();
            return hexCount;
        }
    }

    /// <summary>
    ///   Whether one or more member of this colony is allowed to enter engulf mode.
    /// </summary>
    [JsonIgnore]
    public bool CanEngulf
    {
        get
        {
            if (membersDirty)
                UpdateDerivedProperties();
            return canEngulf;
        }
    }

    /// <summary>
    ///   Total entity weight of the colony. Colony member weights are modified with a multiplier to end up with
    ///   this number;
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that this doesn't include the <see cref="Master"/> weight as the intended use for this property is
    ///     to be read through <see cref="Microbe.EntityWeight"/> where this is added on top for the colony lead cell.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public float EntityWeight
    {
        get
        {
            if (membersDirty)
                UpdateDerivedProperties();
            return entityWeight;
        }
    }

    /// <summary>
    ///   The accumulation of all the colony member's <see cref="Microbe.UsedIngestionCapacity"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This unfortunately is not cached as <see cref="Microbe.UsedIngestionCapacity"/> can change
    ///     every frame.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public float UsedIngestionCapacity => ColonyMembers.Sum(c => c.UsedIngestionCapacity);

    /// <summary>
    ///   Creates a colony for a microbe, with the given microbe as the master,
    ///   and handles related updates (like microbe's colony and access to the editor button).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Should be used instead of the colony constructor, unless for loading from Json.
    ///   </para>
    /// </remarks>
    public static void CreateColonyForMicrobe(Microbe microbe)
    {
        microbe.Colony = new MicrobeColony(microbe);
        microbe.OnColonyMemberAdded(microbe);
    }

    public void Process(float delta)
    {
        _ = delta;

        ColonyCompounds.DistributeCompoundSurplus();
    }

    public void RemoveFromColony(Microbe? microbe)
    {
        if (microbe?.Colony == null)
            throw new ArgumentException("Microbe null or not a member of a colony");

        if (!Equals(microbe.Colony, this))
            throw new ArgumentException("Cannot remove a colony member who isn't a member");

        if (microbe.ColonyChildren == null)
            throw new ArgumentException("Invalid microbe with no colony children setup on it");

        if (State == MicrobeState.Unbinding)
            State = MicrobeState.Normal;

        foreach (var colonyMember in ColonyMembers)
            colonyMember.OnColonyMemberRemoved(microbe);

        microbe.Colony = null;

        microbe.ReParentShapes(microbe, Vector3.Zero);

        while (microbe.ColonyChildren.Count != 0)
            RemoveFromColony(microbe.ColonyChildren[0]);

        ColonyMembers.Remove(microbe);

        microbe.ColonyParent?.ColonyChildren?.Remove(microbe);

        if (microbe.ColonyParent?.Colony != null && microbe.ColonyParent?.ColonyParent == null &&
            microbe.ColonyParent?.ColonyChildren?.Count == 0)
        {
            RemoveFromColony(microbe.ColonyParent);
        }

        microbe.ColonyParent = null;
        microbe.ColonyChildren = null;
        if (microbe != Master)
            Master.Mass -= microbe.Mass;

        membersDirty = true;
    }

    public void AddToColony(Microbe microbe, Microbe master)
    {
        if (microbe == null || master == null || microbe.Colony != null)
            throw new ArgumentException("Microbe or master null or microbe already is in a colony");

        ColonyMembers.Add(microbe);
        Master.Mass += microbe.Mass;

        microbe.ColonyParent = master;
        master.ColonyChildren!.Add(microbe);
        microbe.Colony = this;
        microbe.ColonyChildren = new List<Microbe>();

        ColonyMembers.ForEach(m => m.OnColonyMemberAdded(microbe));

        membersDirty = true;
    }

    private void UpdateDerivedProperties()
    {
        UpdateHexCountAndWeight();
        UpdateCanEngulf();

        membersDirty = false;
    }

    private void UpdateHexCountAndWeight()
    {
        hexCount = 0;
        entityWeight = 0;

        foreach (var member in ColonyMembers)
        {
            hexCount += member.EngulfSize;

            if (member != Master)
                entityWeight += member.EntityWeight * Constants.MICROBE_COLONY_MEMBER_ENTITY_WEIGHT_MULTIPLIER;
        }
    }

    private void UpdateCanEngulf()
    {
        canEngulf = false;

        foreach (var member in ColonyMembers)
        {
            if (!member.CanEngulf)
                continue;

            canEngulf = true;
            break;
        }
    }
}
