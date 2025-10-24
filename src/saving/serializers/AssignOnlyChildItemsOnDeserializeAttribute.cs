using System;

// TODO: remove this entirely once no longer used as placeholder in prototypes

/// <summary>
///   Unused thing, kept as a marker for how to write the proper archiving support as planned for future stages.
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
