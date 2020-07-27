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
    private readonly Dictionary<string, object> referenceToObject = new Dictionary<string, object>();
    private readonly Dictionary<object, string> objectToReference = new Dictionary<object, string>();

    private long referenceCounter;

    public object ResolveReference(object context, string reference)
    {
        return referenceToObject[reference];
    }

    /// <summary>
    ///   Apparently this makes a new reference happen. As well as returns existing ones if there is one
    /// </summary>
    public string GetReference(object context, object value)
    {
        if (objectToReference.ContainsKey(value))
            return objectToReference[value];

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
        objectToReference[value] = reference;
        referenceToObject[reference] = value;
    }
}
