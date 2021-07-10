using System;

/// <summary>
///   When a field or property has this attribute, that field is not entirely assigned on deserialize from JSON,
///   only the sub objects (the fields and properties) of the value is assigned. This is used to copy properties
///   from Godot.Reference derived types to freshly created instances.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AssignOnlyChildItemsOnDeserializeAttribute : Attribute
{
    public AssignOnlyChildItemsOnDeserializeAttribute(bool replaceReferences = true)
    {
        ReplaceReferences = replaceReferences;
    }

    /// <summary>
    ///   When true, the thrive serializer will replace the existing instance in the reference cache when
    ///   just copying properties. This is used to allow references to just child property copied objects to stay valid
    ///   after load from JSON
    /// </summary>
    public bool ReplaceReferences { get; }
}
