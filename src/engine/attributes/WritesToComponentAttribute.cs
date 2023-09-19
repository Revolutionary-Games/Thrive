using System;
using DefaultEcs.System;

/// <summary>
///   Marks a system as writing to a component. <see cref="WithAttribute"/> automatically implies this so this is
///   needed only when the relationship is not clear
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class WritesToComponentAttribute : Attribute
{
    public WritesToComponentAttribute(Type writesTo)
    {
        WritesTo = writesTo;
    }

    public Type WritesTo { get; set; }
}
