using System;
using System.Collections.Generic;
using System.Linq;

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
    ///   The other action replaces the this one
    /// </summary>
    ReplacesOther,

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
    ///   Returns the interference mode with <paramref name="other"/>
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
        if (other is RemoveActionData removeActionData && removeActionData.Organelle.Definition == Organelle.Definition)
        {
            if (removeActionData.Organelle.Position == Organelle.Position)
                return MicrobeActionInterferenceMode.CancelsOut;

            return MicrobeActionInterferenceMode.Combinable;
        }

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Organelle.Definition.MPCost - (ReplacedCytoplasm?.Sum(p => p.Definition.MPCost) ?? 0);
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        var removeActionData = (RemoveActionData)other;
        var oldPosition = removeActionData.Organelle.Position;
        var oldRotation = removeActionData.Organelle.Orientation;
        removeActionData.Organelle.Position = Organelle.Position;
        removeActionData.Organelle.Orientation = Organelle.Orientation;
        return new MoveActionData(removeActionData.Organelle, oldPosition, Organelle.Position, oldRotation,
            Organelle.Orientation);
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
        // If this organelle got placed in this session on the same position
        if (other is PlacementActionData placementActionData &&
            placementActionData.Organelle.Definition == Organelle.Definition)
        {
            if (placementActionData.Organelle.Position == Organelle.Position)
                return MicrobeActionInterferenceMode.CancelsOut;

            return MicrobeActionInterferenceMode.Combinable;
        }

        // If this organelle got moved in this session
        if (other is MoveActionData moveActionData &&
            moveActionData.Organelle.Definition == Organelle.Definition &&
            moveActionData.NewLocation == Organelle.Position)
            return MicrobeActionInterferenceMode.ReplacesOther;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Constants.ORGANELLE_REMOVE_COST;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        var placementActionData = (PlacementActionData)other;
        return new MoveActionData(placementActionData.Organelle,
            Organelle.Position,
            placementActionData.Organelle.Position,
            Organelle.Orientation,
            placementActionData.Organelle.Orientation);
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
        if (other is MoveActionData moveActionData && moveActionData.Organelle.Definition == Organelle.Definition)
        {
            if (OldLocation == moveActionData.NewLocation && NewLocation == moveActionData.OldLocation)
                return MicrobeActionInterferenceMode.CancelsOut;
            if (moveActionData.NewLocation == OldLocation || NewLocation == moveActionData.OldLocation)
                return MicrobeActionInterferenceMode.Combinable;
        }

        // If this organelle got placed in this session
        if (other is PlacementActionData placementActionData && placementActionData.Organelle == Organelle)
            return MicrobeActionInterferenceMode.Combinable;

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return Constants.ORGANELLE_MOVE_COST;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        if (other is PlacementActionData placementActionData)
        {
            placementActionData.Organelle.Position = NewLocation;
            placementActionData.Organelle.Orientation = NewRotation;
            return new PlacementActionData(placementActionData.Organelle)
            {
                ReplacedCytoplasm = placementActionData.ReplacedCytoplasm,
            };
        }

        var moveActionData = (MoveActionData)other;
        if (moveActionData.NewLocation == OldLocation)
        {
            return new MoveActionData(Organelle, moveActionData.OldLocation, NewLocation, moveActionData.OldRotation,
                NewRotation);
        }

        return new MoveActionData(moveActionData.Organelle, OldLocation, moveActionData.NewLocation, OldRotation,
            moveActionData.NewRotation);
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
        {
            if (membraneActionData.NewMembrane == OldMembrane && NewMembrane == membraneActionData.OldMembrane)
                return MicrobeActionInterferenceMode.CancelsOut;

            if (membraneActionData.NewMembrane == OldMembrane || NewMembrane == membraneActionData.OldMembrane)
                return MicrobeActionInterferenceMode.Combinable;
        }

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return NewMembrane.EditorCost;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        var membraneActionData = (MembraneActionData)other;
        if (OldMembrane == membraneActionData.NewMembrane)
            return new MembraneActionData(membraneActionData.OldMembrane, NewMembrane);

        return new MembraneActionData(membraneActionData.NewMembrane, OldMembrane);
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
        if (other is RigidityChangeActionData rigidityChangeActionData)
        {
            if (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON &&
                Math.Abs(rigidityChangeActionData.NewRigidity - PreviousRigidity) < MathUtils.EPSILON)
                return MicrobeActionInterferenceMode.CancelsOut;

            if (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON ||
                Math.Abs(rigidityChangeActionData.NewRigidity - PreviousRigidity) < MathUtils.EPSILON)
                return MicrobeActionInterferenceMode.Combinable;
        }

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return (int)Math.Abs((NewRigidity - PreviousRigidity) * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO) *
            Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
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
