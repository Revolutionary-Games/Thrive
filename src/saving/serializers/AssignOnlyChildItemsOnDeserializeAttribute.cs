using System;

/// <summary>
///   When a field or property has this attribute, that field is not entirely assigned on deserialize from JSON,
///   only the sub objects (the fields and properties) of the value is assigned. This is used to copy properties
///   from Godot.Reference derived types to freshly created instances.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AssignOnlyChildItemsOnDeserializeAttribute : Attribute
{
}
