using System;

/// <summary>
///   Marks a system as needing to run on the main thread where it is allowed to do any Godot engine operations
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class RunsOnMainThreadAttribute : Attribute
{
    public RunsOnMainThreadAttribute(bool requiredToRunOnMain = true)
    {
        RequiredToRunOnMain = requiredToRunOnMain;
    }

    /// <summary>
    ///   If true this system *must* run on the main thread (mostly due to interacting with Godot). When false this
    ///   system is encouraged, but not required to run on the main thread. This is used as a helpful suggestion to
    ///   the threaded run generator to balance threads better.
    /// </summary>
    public bool RequiredToRunOnMain { get; set; }
}
