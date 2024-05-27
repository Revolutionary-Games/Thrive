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
    public RotationEventArgs(Quaternion rotation, Vector3 rotationInRadians)
    {
        Rotation = rotation;
        RotationInRadians = rotationInRadians;
    }

    /// <summary>
    ///   Quaternion of the rotation
    /// </summary>
    public Quaternion Rotation { get; }

    /// <summary>
    ///   Axis-wise rotation in radians
    /// </summary>
    public Vector3 RotationInRadians { get; }
}

public class MicrobeMovementEventArgs : TutorialEventArgs
{
    public MicrobeMovementEventArgs(bool usesScreenRelativeMovement, Vector3 movementDirection)
    {
        UsesScreenRelativeMovement = usesScreenRelativeMovement;
        MovementDirection = movementDirection;
    }

    public bool UsesScreenRelativeMovement { get; private set; }
    public Vector3 MovementDirection { get; private set; }

    /// <summary>
    ///   Reuses this event with new parameters. This method exists to reduce required memory allocations.
    /// </summary>
    public void ReuseEvent(bool usesScreenRelativeMovement, Vector3 movementDirection)
    {
        UsesScreenRelativeMovement = usesScreenRelativeMovement;
        MovementDirection = movementDirection;
    }
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

public class OrganellePlacedEventArgs : TutorialEventArgs
{
    public OrganellePlacedEventArgs(OrganelleDefinition definition)
    {
        Definition = definition;
    }

    public OrganelleDefinition Definition { get; }
}

public class GameWorldEventArgs : TutorialEventArgs
{
    public GameWorldEventArgs(GameWorld world)
    {
        World = world;
    }

    public GameWorld World { get; }
}
