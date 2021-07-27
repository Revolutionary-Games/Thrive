using System;
using System.Collections.Generic;

/// <summary>
///   Describes how two microbe actions interference with each other.
/// </summary>
/// <para>Used for MP calculation</para>
public enum MicrobeActionInterferenceMode
{
    /// <summary>
    ///   The two actions are completely independent
    /// </summary>
    NoInterference,
    /// <summary>
    ///   This action replaces the other one
    /// </summary>
    Replaces,
    /// <summary>
    ///   The two actions cancel out each other
    /// </summary>
    CancelsOut,
    /// <summary>
    ///   The two actions can be combined to a whole different action.
    ///   Call <see cref="MicrobeEditorActionData.Combine"/> to get this action.
    /// </summary>
    Combinable,
}

/// <summary>
///   This is its own interface to make JSON loading dynamic type more strict
/// </summary>
public abstract class MicrobeEditorActionData
{
    /// <summary>
    ///   Does this action cancel out with the <paramref name="other"/> action?
    /// </summary>
    /// <returns>
    ///   Returns the interference mode with <paramref name="other"/>.
    ///   <see cref="MicrobeActionInterferenceMode.Replaces"/> means that <paramref name="other"/> replaces this.
    /// </returns>
    /// <para>Do not call with itself</para>
    public abstract MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other);

    /// <summary>
    ///   Combines two actions to one if possible.
    ///   Call <see cref="MicrobeEditorActionData.GetInterferenceModeWith"/> first and check if it returns <see cref="MicrobeActionInterferenceMode.Combinable"/>
    /// </summary>
    /// <param name="other">The action this should be combined with</param>
    /// <returns>Returns the combined action</returns>
    /// <exception cref="NotSupportedException">Thrown when combination is not possible</exception>
    public MicrobeEditorActionData Combine(MicrobeEditorActionData other)
    {
        if (GetInterferenceModeWith(other) != MicrobeActionInterferenceMode.Combinable)
            throw new NotSupportedException();

        return CombineGuaranteed(other);
    }

    public abstract int CalculateCost();

    /// <summary>
    ///   Combines two actions to one
    /// </summary>
    /// <param name="other">The action this should be combined with. Guaranteed to be combinable</param>
    /// <returns>Returns the combined action</returns>
    protected virtual MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        throw new NotImplementedException();
    }
}

[JSONAlwaysDynamicType]
public class PlacementActionData : MicrobeEditorActionData
{
    public List<OrganelleTemplate> ReplacedCytoplasm;
    public OrganelleTemplate Organelle;

    public PlacementActionData(OrganelleTemplate organelle)
    {
        Organelle = organelle;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        // If this organelle got removed in this session
        if (other is RemoveActionData removeActionData && removeActionData.Organelle == Organelle)
            return MicrobeActionInterferenceMode.CancelsOut;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Organelle.Definition.MPCost;
    }
}

[JSONAlwaysDynamicType]
public class RemoveActionData : MicrobeEditorActionData
{
    public OrganelleTemplate Organelle;

    public RemoveActionData(OrganelleTemplate organelle)
    {
        Organelle = organelle;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        // If this organelle got placed in this session
        if (other is PlacementActionData placementActionData && placementActionData.Organelle == Organelle)
            return MicrobeActionInterferenceMode.CancelsOut;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Constants.ORGANELLE_REMOVE_COST;
    }
}

[JSONAlwaysDynamicType]
public class MoveActionData : MicrobeEditorActionData
{
    public OrganelleTemplate Organelle;
    public Hex OldLocation;
    public Hex NewLocation;
    public int OldRotation;
    public int NewRotation;

    public MoveActionData(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        Organelle = organelle;
        OldLocation = oldLocation;
        NewLocation = newLocation;
        OldRotation = oldRotation;
        NewRotation = newRotation;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        // If this organelle got moved in the same session again
        if (other is MoveActionData moveActionData && moveActionData.Organelle == Organelle)
            return MicrobeActionInterferenceMode.Replaces;

        // If this organelle got placed in this session
        if (other is PlacementActionData placementActionData && placementActionData.Organelle == Organelle)
            return MicrobeActionInterferenceMode.Replaces;

        // If this organelle got removed in this session
        if (other is RemoveActionData removeActionData && removeActionData.Organelle == Organelle)
            return MicrobeActionInterferenceMode.Replaces;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Constants.ORGANELLE_MOVE_COST;
    }
}

[JSONAlwaysDynamicType]
public class MembraneActionData : MicrobeEditorActionData
{
    public MembraneType OldMembrane;
    public MembraneType NewMembrane;

    public MembraneActionData(MembraneType oldMembrane, MembraneType newMembrane)
    {
        OldMembrane = oldMembrane;
        NewMembrane = newMembrane;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        if (other is MembraneActionData membraneActionData)
            return MicrobeActionInterferenceMode.Replaces;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return NewMembrane.EditorCost;
    }
}

[JSONAlwaysDynamicType]
public class RigidityChangeActionData : MicrobeEditorActionData
{
    public float NewRigidity;
    public float PreviousRigidity;

    public RigidityChangeActionData(float newRigidity, float previousRigidity)
    {
        NewRigidity = newRigidity;
        PreviousRigidity = previousRigidity;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        if (other is RigidityChangeActionData rigidityChangeActionData &&
            (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON ||
                Math.Abs(rigidityChangeActionData.NewRigidity - PreviousRigidity) < MathUtils.EPSILON))
        {
            return MicrobeActionInterferenceMode.Combinable;
        }

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return (int)Math.Abs(NewRigidity - PreviousRigidity) * Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        var rigidityChangeActionData = (RigidityChangeActionData)other;

        if (Math.Abs(PreviousRigidity - rigidityChangeActionData.NewRigidity) < MathUtils.EPSILON)
            return new RigidityChangeActionData(NewRigidity, rigidityChangeActionData.PreviousRigidity);

        return new RigidityChangeActionData(rigidityChangeActionData.NewRigidity, PreviousRigidity);
    }
}

[JSONAlwaysDynamicType]
public class NewMicrobeActionData : MicrobeEditorActionData
{
    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public MembraneType OldMembrane;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles, MembraneType oldMembrane)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        OldMembrane = oldMembrane;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return -Constants.BASE_MUTATION_POINTS;
    }
}
