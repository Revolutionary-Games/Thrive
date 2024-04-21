using System.Collections.Generic;

/// <summary>
///   This is its own interface to make JSON loading dynamic type more strict
/// </summary>
#pragma warning disable CA1040 // empty interface
public interface IMicrobeEditorActionData
{
}
#pragma warning restore CA1040

[JSONAlwaysDynamicType]
public class PlacementActionData : IMicrobeEditorActionData
{
    public List<OrganelleTemplate>? ReplacedCytoplasm;
    public OrganelleTemplate Organelle;

    public PlacementActionData(OrganelleTemplate organelle)
    {
        Organelle = organelle;
    }
}

[JSONAlwaysDynamicType]
public class RemoveActionData : IMicrobeEditorActionData
{
    public OrganelleTemplate Organelle;

    public RemoveActionData(OrganelleTemplate organelle)
    {
        Organelle = organelle;
    }
}

[JSONAlwaysDynamicType]
public class MoveActionData : IMicrobeEditorActionData
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
}

[JSONAlwaysDynamicType]
public class BehaviourChangeActionData : IMicrobeEditorActionData
{
    public float NewValue;
    public float OldValue;
    public BehaviouralValueType Type;

    public BehaviourChangeActionData(float newValue, float oldValue, BehaviouralValueType type)
    {
        NewValue = newValue;
        OldValue = oldValue;
        Type = type;
    }
}

[JSONAlwaysDynamicType]
public class RigidityChangeActionData : IMicrobeEditorActionData
{
    public float NewRigidity;
    public float PreviousRigidity;

    public RigidityChangeActionData(float newRigidity, float previousRigidity)
    {
        NewRigidity = newRigidity;
        PreviousRigidity = previousRigidity;
    }
}
