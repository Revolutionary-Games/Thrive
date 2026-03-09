namespace Components;

using System;
using Godot;
using SharedBase.Archive;

public struct SpatialAnimation
{
    public Vector3 InitialPosition;
    public Vector3 FinalPosition;

    public Vector3 InitialScale;
    public Vector3 FinalScale;

    public float AnimationTime;
    public float TimeSpent;

    public SpatialAnimation(Vector3 initialPosition, Vector3 finalPosition, Vector3 initialScale, Vector3 finalScale)
    {
        InitialPosition = initialPosition;
        FinalPosition = finalPosition;
        InitialScale = initialScale;
        FinalScale = finalScale;

        AnimationTime = 1.0f;
    }
}
