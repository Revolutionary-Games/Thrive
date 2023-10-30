using System;
using DefaultEcs.System;

/// <summary>
///   Marks a system as reading from a component. Can be used to mark a <see cref="WithAttribute"/> relationship
///   as read only or mark a component the system might read.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class ReadsComponentAttribute : Attribute
{
    public ReadsComponentAttribute(Type readsFrom)
    {
        ReadsFrom = readsFrom;
    }

    public Type ReadsFrom { get; set; }
}
