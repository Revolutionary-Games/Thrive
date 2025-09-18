using System;
using Arch.System.SourceGenerator;

/// <summary>
///   Marks a system as reading from a component. Can be used to mark a <see cref="AllAttribute{T0}"/> relationship
///   as read-only or mark a component the system might read.
/// </summary>
/// <remarks>
///   <para>
///     Note that in situations where systems do only light writes, for example, to properties exclusively reserved
///     for that system, this attribute is used to communicate that to the threaded run generator.
///     Or this can be used for really specific fields inside contained objects of a component.
///   </para>
///   <para>
///     For component types that are usually marked with this even though they are written to are
///     <see cref="Components.Health"/> and <see cref="Components.SoundEffectPlayer"/> as they have thread safe methods
///     to queue data actions into them.
///     That is marked with <see cref="ComponentIsReadByDefaultAttribute"/>.
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class ReadsComponentAttribute : Attribute
{
    public ReadsComponentAttribute(Type readsFrom)
    {
        ReadsFrom = readsFrom;
    }

    public Type ReadsFrom { get; set; }
}
