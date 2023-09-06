using System;

/// <summary>
///   Marks a system as running on each rendered frame rather than on logic update (most usually 60 times per second)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class RunsOnFrameAttribute : Attribute
{
}
