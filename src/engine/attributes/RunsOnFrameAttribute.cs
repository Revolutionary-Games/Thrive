using System;

/// <summary>
///   Marks a system as running on each rendered frame rather than on logic update (logic updates run most usually
///   60 times per second at most)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class RunsOnFrameAttribute : Attribute
{
}
