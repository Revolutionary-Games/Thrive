using System;

/// <summary>
///   Marks a system as needing to run on the main thread where it is allowed to do any Godot engine operations
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class RunsOnMainThreadAttribute : Attribute
{
}
