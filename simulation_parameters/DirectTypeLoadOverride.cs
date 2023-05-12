using System;

/// <summary>
///   Needed to make loading registry type definitions from JSON to work, this overwrites the use of string key based
///   registry type loading for a specific type
/// </summary>
public class DirectTypeLoadOverride : BaseThriveConverter
{
    private readonly Type typeToOverrideLoadingFor;

    public DirectTypeLoadOverride(Type typeToOverrideLoadingFor, ISaveContext? context) : base(context)
    {
        this.typeToOverrideLoadingFor = typeToOverrideLoadingFor;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeToOverrideLoadingFor.IsAssignableFrom(objectType);
    }
}
