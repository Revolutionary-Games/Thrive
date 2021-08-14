using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base type for tutorial event arguments
/// </summary>
public class TutorialEventArgs : EventArgs
{
}

public class MicrobeEventArgs : TutorialEventArgs
{
    public MicrobeEventArgs(Microbe microbe)
    {
        Microbe = microbe;
    }

    public Microbe Microbe { get; }
}

public class RotationEventArgs : TutorialEventArgs
{
    public RotationEventArgs(Basis rotation, Vector3 rotationInDegrees)
    {
        Rotation = rotation;
        RotationInDegrees = rotationInDegrees;
    }

    /// <summary>
    ///   Quaternion of the rotation
    /// </summary>
    public Basis Rotation { get; }

    /// <summary>
    ///   Axis-wise degree rotations
    /// </summary>
    public Vector3 RotationInDegrees { get; }
}

public class CompoundPositionEventArgs : TutorialEventArgs
{
    public CompoundPositionEventArgs(Vector3? glucosePosition)
    {
        GlucosePosition = glucosePosition;
    }

    public Vector3? GlucosePosition { get; }
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
    public StringEventArgs(string data)
    {
        Data = data;
    }

    public string Data { get; }
}

public class PatchEventArgs : TutorialEventArgs
{
    public PatchEventArgs(Patch patch)
    {
        Patch = patch;
    }

    public Patch Patch { get; }
}

public class CallbackEventArgs : TutorialEventArgs
{
    public CallbackEventArgs(Action data)
    {
        Data = data;
    }

    public Action Data { get; }
}
