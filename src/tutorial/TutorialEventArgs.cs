using System;
using System.Collections.Generic;
using DefaultEcs;
using Godot;

/// <summary>
///   Base type for tutorial event arguments
/// </summary>
public class TutorialEventArgs : EventArgs
{
}

public class MicrobeEventArgs : TutorialEventArgs
{
    public MicrobeEventArgs(Entity microbe)
    {
        Microbe = microbe;
    }

    public Entity Microbe { get; }
}

public class RotationEventArgs : TutorialEventArgs
{
    public RotationEventArgs(Quat rotation, Vector3 rotationInDegrees)
    {
        Rotation = rotation;
        RotationInDegrees = rotationInDegrees;
    }

    /// <summary>
    ///   Quaternion of the rotation
    /// </summary>
    public Quat Rotation { get; }

    /// <summary>
    ///   Axis-wise degree rotations
    /// </summary>
    public Vector3 RotationInDegrees { get; }
}

public class MicrobeMovementEventArgs : TutorialEventArgs
{
    public MicrobeMovementEventArgs(bool usesScreenRelativeMovement, Vector3 movementDirection)
    {
        UsesScreenRelativeMovement = usesScreenRelativeMovement;
        MovementDirection = movementDirection;
    }

    public bool UsesScreenRelativeMovement { get; }
    public Vector3 MovementDirection { get; }
}

public class EntityPositionEventArgs : TutorialEventArgs
{
    public EntityPositionEventArgs(Vector3? position)
    {
        EntityPosition = position;
    }

    public Vector3? EntityPosition { get; }
}

public class CompoundBagEventArgs : TutorialEventArgs
{
    public CompoundBagEventArgs(CompoundBag compounds)
    {
        Compounds = compounds;
    }

    public CompoundBag Compounds { get; }
}

public class CompoundEventArgs : TutorialEventArgs
{
    public CompoundEventArgs(Dictionary<Compound, float> compounds)
    {
        Compounds = compounds;
    }

    public Dictionary<Compound, float> Compounds { get; }
}

public class StringEventArgs : TutorialEventArgs
{
    public StringEventArgs(string? data)
    {
        Data = data;
    }

    public string? Data { get; }
}

public class PatchEventArgs : TutorialEventArgs
{
    public PatchEventArgs(Patch? patch)
    {
        Patch = patch;
    }

    public Patch? Patch { get; }
}

public class CallbackEventArgs : TutorialEventArgs
{
    public CallbackEventArgs(Action data)
    {
        Data = data;
    }

    public Action Data { get; }
}

public class MicrobeColonyEventArgs : TutorialEventArgs
{
    public MicrobeColonyEventArgs(bool hasColony, int memberCount, bool isMulticellular)
    {
        HasColony = hasColony;
        MemberCount = memberCount;
        IsMulticellular = isMulticellular;
    }

    public bool HasColony { get; }
    public int MemberCount { get; }
    public bool IsMulticellular { get; }
}

public class EnergyBalanceEventArgs : TutorialEventArgs
{
    public EnergyBalanceEventArgs(EnergyBalanceInfo energyBalanceInfo)
    {
        EnergyBalanceInfo = energyBalanceInfo;
    }

    public EnergyBalanceInfo EnergyBalanceInfo { get; }
}
