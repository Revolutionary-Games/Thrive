using System;
using DefaultEcs.System;

/// <summary>
///   Marks a system as writing to a component. <see cref="WithAttribute"/> automatically implies this so this is
///   needed only when the relationship is not clear. Writing also implies reading so when this is added with a
///   specific component, <see cref="ReadsComponentAttribute"/> should <b>not</b> be added with the same component
///   type.
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
