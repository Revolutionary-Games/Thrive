using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Serialization;

/// <summary>
///   Reference resolver for JSON as there doesn't seem to be a default one (their default one is marked internal)
/// </summary>
/// <remarks>
///   <para>
///     Determine if the context should be used or not
///   </para>
/// </remarks>
public class ReferenceResolver : IReferenceResolver
{
    private readonly Dictionary<string, object> referenceToObject = new();

    /// <summary>
    ///   Dictionary mapping objects to their JSON references.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     JSON references stored by this represent object identity, not value equality.
    ///     In particular, mutable editor clones can be equal to their source objects while still needing independent
    ///     references.
    ///   </para>
    /// </remarks>
    private readonly Dictionary<object, string> objectToReference = new(ReferenceEqualityComparer.Instance);

    private long referenceCounter;

    public object ResolveReference(object context, string reference)
    {
        if (!referenceToObject.TryGetValue(reference, out var referencedObject))
        {
            throw new KeyNotFoundException($"The reference {reference} was not found. " +
                "Is a child referencing an ancestor? If so, you should add [UseThriveSerializer] " +
                "and make sure property order is sensible");
        }

        return referencedObject;
    }

    /// <summary>
    ///   Apparently this makes a new reference happen. As well as returns existing ones if there is one
    /// </summary>
    public string GetReference(object context, object value)
    {
        if (objectToReference.TryGetValue(value, out var existing))
            return existing;

        string reference = (++referenceCounter).ToString(CultureInfo.InvariantCulture);

        objectToReference[value] = reference;
        referenceToObject[reference] = value;

        return reference;
    }

    public bool IsReferenced(object context, object value)
    {
        return objectToReference.ContainsKey(value);
    }

    public void AddReference(object context, string reference, object value)
    {
        // TODO: when replacing references this first assignment doesn't overwrite the old value, so an outdated
        // reference is left. Should that be removed?
        objectToReference[value] = reference;
        referenceToObject[reference] = value;
    }
}
