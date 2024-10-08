﻿using Godot;

/// <summary>
///   Base photographable type for things that can be photographed. See <see cref="IScenePhotographable"/> for example.
/// </summary>
public interface IPhotographable<T>
{
    /// <summary>
    ///   Calculates where the camera is when photographing this.
    /// </summary>
    /// <param name="photographableObjectState">
    ///   Access to the current photo state for determining things related to the photographed object
    /// </param>
    /// <returns>
    ///   The position of the camera. Often only the Y-position needs to be set to pick a good distance to
    ///   photograph from
    /// </returns>
    public Vector3 CalculatePhotographDistance(T photographableObjectState);

    /// <summary>
    ///   Calculates a visual hash code for this photographable. This needs to be a good hash as otherwise generated
    ///   images may be re-used incorrectly.
    /// </summary>
    /// <returns>Hash code for this</returns>
    public ulong GetVisualHashCode();
}
