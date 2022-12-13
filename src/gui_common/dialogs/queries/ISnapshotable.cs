using Godot;
using System;

/// <summary>
///   An interface for all UI elements that store snapshots
/// </summary>
/// <remarks>
///   This interface is only intended for temporarily stored snapshots.
///   Configuration UI should not use it but rather use storing files.
/// </remarks>
public interface ISnapshotable
{
    public void MakeSnapshot();
    public void RestoreLastSnapshot();
}
